"""
cache.py — In-Memory Artifact Store
====================================
Central module that holds all pre-trained ML artifacts in process memory.
All inference functions read from these dicts/lists directly — zero disk I/O
per request after startup.

Lifecycle
---------
1.  On startup  → `load_all_artifacts()` is called by the FastAPI lifespan hook.
    It reads every artifact file that exists on disk and populates the stores below.
    Missing files are silently skipped (models not yet trained).

2.  After training → the background task calls `reload_artifact(key)` (or
    `load_all_artifacts()`) so the new artifact is immediately hot in memory
    without restarting the server.

3.  Inference functions → import the store constants and read directly.
    No file handles, no json.load(), no joblib.load() per request.

Store layout
------------
    RECOMMENDATION_INDEX : dict[str, list[str]]
        { antecedent_sku : [consequent_sku, ...] }   — top-N already ordered by lift

    CLUSTER_STORE : dict[int, list[dict]]
        { cluster_id : [ ClusterItem-dict, ... ] }

    FORECAST_CACHE : dict[str, dict]
        { sku : { "dates": [...], "yhat": [...], ... } }

    PROPHET_MODELS : dict[str, Prophet]
        { sku : <fitted Prophet model> }
        Kept separately so `get_reorder_alerts` can call model.predict()
        without touching disk. Forecasts are also written to FORECAST_CACHE
        after training so the common GET /forecast/{sku} path never touches
        a model object at all.

    TRAINING_STATUS : dict
        Metadata written by the background training task so the
        GET /api/v1/training-status endpoint can report progress.
"""

from __future__ import annotations

import json
import logging
import os
import time
from collections import defaultdict
from typing import Any

import joblib

from api.config import (
    CLUSTERING_RESULT_FILE,
    FP_GROWTH_RULES_FILE,
    PROPHET_MODELS_DIR,
)

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# In-memory stores  (module-level singletons)
# ---------------------------------------------------------------------------

# FP-Growth: antecedent → ordered list of consequents
RECOMMENDATION_INDEX: dict[str, list[str]] = {}

# K-Means: cluster_id (int) → list of product dicts
CLUSTER_STORE: dict[int, list[dict]] = {}

# Prophet pre-computed forecasts: sku → forecast payload
FORECAST_CACHE: dict[str, dict] = {}

# Prophet model objects: sku → fitted model (kept for reorder alert demand sum)
PROPHET_MODELS: dict[str, Any] = {}

# Training progress metadata
TRAINING_STATUS: dict[str, Any] = {
    "status": "not_started",       # not_started | running | completed | partial_failure
    "started_at": None,
    "completed_at": None,
    "modules": {
        "fp_growth":   {"status": "pending", "error": None},
        "clustering":  {"status": "pending", "error": None},
        "prophet":     {"status": "pending", "skus_trained": 0, "skus_failed": 0, "error": None},
    },
}

# ---------------------------------------------------------------------------
# Loaders  (each loads exactly one artifact type)
# ---------------------------------------------------------------------------

def _load_recommendations() -> None:
    """
    Build RECOMMENDATION_INDEX from the saved FP-Growth rules JSON.

    Original pattern (broken per-request):
        with open(file) as f: rules = json.load(f)
        for rule in rules:
            if rule['antecedents'].lower() == sku.lower(): ...

    This loader runs once and builds a dict so lookup is O(1):
        RECOMMENDATION_INDEX["milk"] = ["bread", "butter", ...]
    """
    global RECOMMENDATION_INDEX

    if not os.path.exists(FP_GROWTH_RULES_FILE):
        logger.warning("FP-Growth rules file not found — recommendations unavailable.")
        return

    with open(FP_GROWTH_RULES_FILE, "r", encoding="utf-8") as f:
        rules: list[dict] = json.load(f)

    index: dict[str, list[str]] = defaultdict(list)
    seen: dict[str, set[str]] = defaultdict(set)      # dedup per antecedent

    for rule in rules:
        ant = rule.get("antecedents", "")
        con = rule.get("consequents", "")
        if not ant or not con:
            continue
        ant_key = ant.lower()
        if con not in seen[ant_key]:
            index[ant_key].append(con)
            seen[ant_key].add(con)

    RECOMMENDATION_INDEX = dict(index)
    logger.info("Recommendation index loaded: %d antecedent SKUs.", len(RECOMMENDATION_INDEX))


def _load_clusters() -> None:
    """
    Build CLUSTER_STORE from the saved K-Means JSON.

    Original pattern (broken per-request):
        data = json.load(f)
        for item in data: clusters[int(item['Cluster'])].append(item)

    This loader runs once; lookups are O(1) dict access.
    """
    global CLUSTER_STORE

    if not os.path.exists(CLUSTERING_RESULT_FILE):
        logger.warning("Cluster result file not found — clusters unavailable.")
        return

    with open(CLUSTERING_RESULT_FILE, "r", encoding="utf-8") as f:
        data: list[dict] = json.load(f)

    store: dict[int, list[dict]] = defaultdict(list)
    for item in data:
        c_id = int(item["Cluster"])
        store[c_id].append(item)

    CLUSTER_STORE = dict(store)
    logger.info("Cluster store loaded: %d clusters, %d total SKUs.", len(CLUSTER_STORE), len(data))


def _load_prophet_models() -> None:
    """
    Deserialize every Prophet .pkl in PROPHET_MODELS_DIR into PROPHET_MODELS.
    Also pre-computes and populates FORECAST_CACHE so GET /forecast/{sku}
    never touches a model object or disk at request time.
    """
    global PROPHET_MODELS, FORECAST_CACHE

    if not os.path.isdir(PROPHET_MODELS_DIR):
        logger.warning("Prophet models directory not found — forecasts unavailable.")
        return

    pkl_files = [f for f in os.listdir(PROPHET_MODELS_DIR) if f.endswith("_model.pkl")]

    if not pkl_files:
        logger.warning("No Prophet model files found in %s.", PROPHET_MODELS_DIR)
        return

    loaded = 0
    for fname in pkl_files:
        sku = fname.replace("_model.pkl", "")
        model_path = os.path.join(PROPHET_MODELS_DIR, fname)
        try:
            model = joblib.load(model_path)
            PROPHET_MODELS[sku] = model

            import pandas as pd
            # Fix: Predict exactly 30 days starting from tomorrow
            tomorrow = pd.Timestamp.today().normalize() + pd.Timedelta(days=1)
            future_dates = pd.date_range(start=tomorrow, periods=30, freq='D')
            future = pd.DataFrame({'ds': future_dates})
            
            forecast = model.predict(future)
            
            # Note: _safe_forecast_values from services.py is not imported here, 
            # but we can apply the same reverse log1p directly here.
            import numpy as np
            def safe_rev(x):
                t = np.expm1(x).clip(lower=0)
                return np.where(t < 0.5, 0.0, t)

            recent = forecast.tail(30).copy()
            recent["yhat"] = safe_rev(recent["yhat"])
            recent["yhat_lower"] = safe_rev(recent["yhat_lower"])
            recent["yhat_upper"] = safe_rev(recent["yhat_upper"])
            FORECAST_CACHE[sku] = {
                "dates":      recent["ds"].dt.strftime("%Y-%m-%d").tolist(),
                "yhat":       recent["yhat"].tolist(),
                "yhat_lower": recent["yhat_lower"].tolist(),
                "yhat_upper": recent["yhat_upper"].tolist(),
            }
            loaded += 1
        except Exception as exc:
            logger.error("Failed to load Prophet model for SKU '%s': %s", sku, exc)

    logger.info("Prophet models loaded: %d / %d files.", loaded, len(pkl_files))


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def load_all_artifacts() -> None:
    """
    Load every artifact type from disk into memory.
    Called once by the FastAPI lifespan hook at server startup.
    Failures in one module do not block the others.
    """
    logger.info("Loading ML artifacts into memory...")
    t0 = time.perf_counter()

    _load_recommendations()
    _load_clusters()
    _load_prophet_models()

    elapsed = time.perf_counter() - t0
    logger.info("All artifacts loaded in %.2fs.", elapsed)


def reload_artifact(key: str) -> None:
    """
    Hot-reload a single artifact type after training completes.
    Call this from the background training task so the new model is
    immediately available without a server restart.

    Args:
        key: one of "fp_growth" | "clustering" | "prophet"
    """
    loaders = {
        "fp_growth":  _load_recommendations,
        "clustering": _load_clusters,
        "prophet":    _load_prophet_models,
    }
    loader = loaders.get(key)
    if loader is None:
        raise ValueError(f"Unknown artifact key: '{key}'. Must be one of {list(loaders)}.")
    logger.info("Hot-reloading artifact: %s", key)
    loader()