"""
main.py — FastAPI Application Entry Point
==========================================
Key changes vs the original:

1.  lifespan hook (replaces @app.on_event which is deprecated):
    Calls cache.load_all_artifacts() on startup so every inference endpoint
    is hot from the first request. No cold-start disk I/O per GET.

2.  Background training now writes to cache.TRAINING_STATUS so the frontend
    can poll GET /api/v1/training-status instead of guessing.

3.  GET /api/v1/recommendations/{sku} now accepts ?top_n= query param.

4.  GET / returns a richer health-check payload.
"""

from __future__ import annotations

import time
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException, BackgroundTasks, Query
import uvicorn

import api.cache as cache
from api.models import (
    ForecastResponse,
    RecommendationResponse,
    ClusterResponse,
    ReorderAlertResponse,
    TrainResponse,
    TrainingStatusResponse,   # new — added to models.py
)
from api.services import (
    train_fp_growth,
    train_clustering,
    train_prophet_for_product,
    get_recommendations,
    get_clusters,
    get_forecast,
    get_reorder_alerts,
)
from api.data_handler import load_inventory_data


# ---------------------------------------------------------------------------
# Lifespan hook — replaces deprecated @app.on_event("startup")
# ---------------------------------------------------------------------------

@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Runs once when the server starts.
    Loads all pre-trained artifacts from disk into memory so every
    inference endpoint is served from RAM from the very first request.
    """
    cache.load_all_artifacts()
    yield
    # (shutdown logic can go here if needed in future)


# ---------------------------------------------------------------------------
# App definition
# ---------------------------------------------------------------------------

app = FastAPI(
    title="Supermarket Inventory AI Service",
    description=(
        "Microservice providing demand forecasting, market basket analysis, "
        "product clustering, and reorder alerts."
    ),
    version="2.0.0",
    lifespan=lifespan,
)


# ---------------------------------------------------------------------------
# Health check
# ---------------------------------------------------------------------------

@app.get("/")
def read_root():
    """Rich health-check: reports which artifact types are currently loaded."""
    return {
        "status": "ok",
        "version": "2.0.0",
        "artifacts_loaded": {
            "fp_growth_rules":  len(cache.RECOMMENDATION_INDEX),
            "cluster_skus":     sum(len(v) for v in cache.CLUSTER_STORE.values()),
            "prophet_forecasts": len(cache.FORECAST_CACHE),
        },
        "training_status": cache.TRAINING_STATUS["status"],
    }


# ---------------------------------------------------------------------------
# Training endpoints
# ---------------------------------------------------------------------------

def _update_global_training_status():
    status = cache.TRAINING_STATUS
    module_statuses = {m["status"] for m in status["modules"].values()}
    if "running" in module_statuses:
        status["status"] = "running"
    elif "pending" in module_statuses:
        status["status"] = "queued"
    elif "failed" in module_statuses and "completed" in module_statuses:
        status["status"] = "partial_failure"
    elif "failed" in module_statuses and "completed" not in module_statuses:
        status["status"] = "failed"
    else:
        status["status"] = "completed"
        status["completed_at"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())

def _run_fp_growth_training() -> None:
    status = cache.TRAINING_STATUS
    status["modules"]["fp_growth"]["status"] = "running"
    _update_global_training_status()
    try:
        train_fp_growth()
        status["modules"]["fp_growth"]["status"] = "completed"
    except Exception as exc:
        status["modules"]["fp_growth"]["status"] = "failed"
        status["modules"]["fp_growth"]["error"] = str(exc)
    finally:
        _update_global_training_status()

def _run_clustering_training() -> None:
    status = cache.TRAINING_STATUS
    status["modules"]["clustering"]["status"] = "running"
    _update_global_training_status()
    try:
        train_clustering()
        status["modules"]["clustering"]["status"] = "completed"
    except Exception as exc:
        status["modules"]["clustering"]["status"] = "failed"
        status["modules"]["clustering"]["error"] = str(exc)
    finally:
        _update_global_training_status()

def _run_prophet_training(top_n_prophet: int = 50) -> None:
    status = cache.TRAINING_STATUS
    status["modules"]["prophet"]["status"] = "running"
    _update_global_training_status()
    try:
        df = load_inventory_data()
        if not df.empty:
            top_skus = df["sku"].value_counts().head(top_n_prophet).index
            trained, failed = 0, 0
            for sku in top_skus:
                try:
                    train_prophet_for_product(sku)
                    trained += 1
                except Exception as sku_exc:
                    failed += 1
                    errors = status["modules"]["prophet"].get("error") or ""
                    status["modules"]["prophet"]["error"] = errors + f"{sku}: {sku_exc} | "
            status["modules"]["prophet"]["skus_trained"] = trained
            status["modules"]["prophet"]["skus_failed"] = failed
        status["modules"]["prophet"]["status"] = "completed"
    except Exception as exc:
        status["modules"]["prophet"]["status"] = "failed"
        status["modules"]["prophet"]["error"] = str(exc)
    finally:
        _update_global_training_status()

@app.post("/api/v1/train/fp-growth", response_model=TrainResponse)
def trigger_fp_growth_training(background_tasks: BackgroundTasks):
    """Triggers background training of the FP-Growth model."""
    cache.TRAINING_STATUS["modules"]["fp_growth"]["status"] = "pending"
    cache.TRAINING_STATUS["modules"]["fp_growth"]["error"] = None
    cache.TRAINING_STATUS["started_at"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    _update_global_training_status()
    background_tasks.add_task(_run_fp_growth_training)
    return TrainResponse(
        message="Training started in the background for FP-Growth.",
        status="queued",
    )

@app.post("/api/v1/train/clustering", response_model=TrainResponse)
def trigger_clustering_training(background_tasks: BackgroundTasks):
    """Triggers background training of the Clustering model."""
    cache.TRAINING_STATUS["modules"]["clustering"]["status"] = "pending"
    cache.TRAINING_STATUS["modules"]["clustering"]["error"] = None
    cache.TRAINING_STATUS["started_at"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    _update_global_training_status()
    background_tasks.add_task(_run_clustering_training)
    return TrainResponse(
        message="Training started in the background for Clustering.",
        status="queued",
    )

@app.post("/api/v1/train/prophet", response_model=TrainResponse)
def trigger_prophet_training(
    background_tasks: BackgroundTasks,
    top_n_prophet: int = Query(default=50, ge=1, le=500, description="Number of top SKUs to train Prophet models for.")
):
    """Triggers background training of the Prophet model."""
    cache.TRAINING_STATUS["modules"]["prophet"]["status"] = "pending"
    cache.TRAINING_STATUS["modules"]["prophet"]["error"] = None
    cache.TRAINING_STATUS["modules"]["prophet"]["skus_trained"] = 0
    cache.TRAINING_STATUS["modules"]["prophet"]["skus_failed"] = 0
    cache.TRAINING_STATUS["started_at"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
    _update_global_training_status()
    background_tasks.add_task(_run_prophet_training, top_n_prophet)
    return TrainResponse(
        message=f"Training started in the background for top {top_n_prophet} SKUs.",
        status="queued",
    )


@app.get("/api/v1/training-status", response_model=TrainingStatusResponse)
def get_training_status():
    """
    Returns the current training progress.
    Frontend should poll this after calling POST /train-models.
    """
    return TrainingStatusResponse(**cache.TRAINING_STATUS)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    uvicorn.run("api.main:app", host="127.0.0.1", port=8000, reload=True)