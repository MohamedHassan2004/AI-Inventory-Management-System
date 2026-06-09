"""
models.py — Pydantic Response Models
======================================
Fixes in this version
---------------------
#15 — ClusterItem fields renamed to snake_case (price, frequency, cluster_id)
      to match the rest of the schema (sku, quantity, orderId).
      Field(alias=...) lets Pydantic still accept the capitalized names that
      come directly off the DataFrame dict, so no changes are needed in the
      clustering business logic.

#16 — ClusterResponse changed from Dict[int, List[ClusterItem]] to a flat
      list of ClusterGroup objects, each carrying an explicit integer
      cluster_id field.

      OLD shape (broken):
          { "clusters": { "0": [...], "1": [...] } }
          — JSON always stringifies dict keys, so int 0 → "0", int 1 → "1"
          — Frontend had to parseInt every key before using it
          — Undocumented surprise for the React team

      NEW shape (correct):
          { "clusters": [ { "cluster_id": 0, "items": [...] }, ... ] }
          — cluster_id is an explicit typed integer field, never a string key
          — Deterministically sorted by cluster_id ascending
          — React can just do: clusters.map(({ cluster_id, items }) => ...)
"""

from __future__ import annotations

from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field


# ---------------------------------------------------------------------------
# Forecast
# ---------------------------------------------------------------------------

class ForecastResponse(BaseModel):
    sku: str
    forecast_dates: List[str]
    forecast_values: List[float]
    lower_bounds: List[float]
    upper_bounds: List[float]


# ---------------------------------------------------------------------------
# Recommendations
# ---------------------------------------------------------------------------

class RecommendationResponse(BaseModel):
    sku: str
    recommendations: List[str]


# ---------------------------------------------------------------------------
# Clustering  (fixes #15 + #16)
# ---------------------------------------------------------------------------

class ClusterItem(BaseModel):
    """
    One SKU's cluster data.

    Field names are snake_case in the JSON response (#15).
    Field(alias=...) allows construction directly from the DataFrame dict
    which still uses the original capitalized column names — no changes
    needed in services.py or cache.py.

    Uses Pydantic v2 inner Config with populate_by_name=True.
    """

    sku:        str
    quantity:   float
    price:      float = Field(alias="Price")
    frequency:  int   = Field(alias="Frequency")
    cluster_id: int   = Field(alias="Cluster")

    class Config:
        # Allow callers to use either the snake_case field name OR the alias
        # when constructing the model.  Without this, only the alias works
        # when an alias is defined.
        populate_by_name = True


class ClusterGroup(BaseModel):
    """
    One cluster bucket — an explicit integer ID plus its member SKUs.
    Replaces the implicit Dict[int, ...] key which JSON serializes to a string.
    """
    cluster_id: int
    items:      List[ClusterItem]


class ClusterResponse(BaseModel):
    """
    Fix #16: clusters is now a List[ClusterGroup], sorted by cluster_id.

    Frontend consumption (React):
        const { clusters } = await fetch('/api/v1/clusters').then(r => r.json())
        clusters.forEach(({ cluster_id, items }) => { ... })

    No parseInt, no string-key gymnastics.
    """
    clusters: List[ClusterGroup]


# ---------------------------------------------------------------------------
# Reorder Alerts
# ---------------------------------------------------------------------------

class ReorderAlert(BaseModel):
    sku:                        str
    quantity:                   float
    predicted_demand_next_week: float
    reorder_status:             str
    suggested_order_qty:        float


class ReorderAlertResponse(BaseModel):
    alerts: List[ReorderAlert]


# ---------------------------------------------------------------------------
# Training
# ---------------------------------------------------------------------------

class TrainResponse(BaseModel):
    message: str
    status:  str


class ModuleStatus(BaseModel):
    status:       str
    error:        Optional[str] = None
    skus_trained: Optional[int] = None
    skus_failed:  Optional[int] = None


class TrainingStatusResponse(BaseModel):
    status:       str
    started_at:   Optional[str] = None
    completed_at: Optional[str] = None
    modules:      Dict[str, Any]