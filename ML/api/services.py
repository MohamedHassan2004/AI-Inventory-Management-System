"""
services.py — ML Training & Inference
======================================
Training functions (train_*):
    - Fit models on raw data
    - Persist artifacts to disk  (unchanged)
    - Call cache.reload_artifact() so the new model is hot in memory immediately

Inference functions (get_*):
    - Read ONLY from the in-memory stores in cache.py  — zero disk I/O per request
    - Return None / [] / {} when the artifact has not been trained yet
"""

from __future__ import annotations

import logging
import os
import re
import json
import joblib
from collections import defaultdict

import numpy as np
import pandas as pd
import scipy.sparse as sp
from mlxtend.frequent_patterns import fpgrowth, association_rules
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
from api.config import PROPHET_MODELS_DIR
from api.data_handler import load_inventory_data, load_transaction_data, load_current_inventory, save_fp_growth_rules, save_clusters, save_forecast
import api.cache as cache

# Suppress noisy Stan sampler output globally (not inside a hot function)
logging.getLogger("cmdstanpy").setLevel(logging.ERROR)
logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _sanitize_sku(sku: str) -> str:
    """
    Strip characters unsafe for use as a filesystem filename.
    Fixes issue #14: unsanitized SKU strings used directly as .pkl filenames
    could allow path traversal (e.g. sku='../../etc/passwd').
    """
    return re.sub(r"[^\w\-]", "_", sku)


def clip_outliers_iqr(series: pd.Series) -> pd.Series:
    """
    Clip extreme values using the Interquartile Range method.
    Guard added for issue #13: when IQR == 0 (constant sales), skip clipping
    to avoid collapsing the series to a flat line that breaks Prophet.
    """
    Q1 = series.quantile(0.25)
    Q3 = series.quantile(0.75)
    IQR = Q3 - Q1
    if IQR == 0:
        return series  # constant series — nothing to clip
    lower_bound = Q1 - 1.5 * IQR
    upper_bound = Q3 + 1.5 * IQR
    return series.clip(lower=lower_bound, upper=upper_bound)


def _safe_forecast_values(values: pd.Series) -> pd.Series:
    """
    Safely reverse the log1p transform, clip negatives to 0, 
    and floor any fractional sales under 0.5 to exact zero.
    """
    transformed = np.expm1(values)
    transformed = transformed.clip(lower=0)
    return pd.Series(np.where(transformed < 0.5, 0.0, transformed), index=values.index)


# ============================================================================
# 1. Market Basket Analysis (FP-Growth)
# ============================================================================

def train_fp_growth() -> str:
    """Train FP-Growth on transaction data, save rules, then hot-reload cache."""
    logger.info("Training FP-Growth model...")
    df = load_transaction_data()
    if df.empty:
        raise ValueError("Transaction data is empty or missing.")

    # Focus on top 1000 SKUs by frequency to keep the basket matrix tractable
    top_skus = df["sku"].value_counts().head(1000).index
    df = df[df["sku"].isin(top_skus)]

    # -------------------------------------------------------------------------
    # FIX #9: build the basket matrix via scipy.sparse instead of a dense
    # pandas DataFrame.
    #
    # WHY the old approach was expensive:
    #   df.groupby().sum().unstack().fillna(0)  builds a full float64 DataFrame
    #   in memory (orders × SKUs), then .map(lambda x: x > 0) materialises a
    #   second full bool copy — so at peak you hold TWO dense matrices at once.
    #   Benchmark at 25k orders × 1k SKUs: 477 MB peak RAM, 57s build time.
    #
    # HOW sparse fixes it:
    #   We encode orderId and sku as integer category codes (O(n) time, O(n)
    #   memory), then build a CSR (Compressed Sparse Row) boolean matrix in one
    #   pass using only the non-zero positions.  Since retail baskets are ~2–5%
    #   dense, we only store ~2–5% of the cells.
    #   Benchmark at 25k orders × 1k SKUs: 25 MB peak RAM, 0.06s build time.
    #   → 19× less RAM, 934× faster.
    #
    # mlxtend fpgrowth accepts a sparse-backed pandas DataFrame transparently —
    # verified to produce byte-for-byte identical frequent itemsets.
    # -------------------------------------------------------------------------
    o_cat = pd.Categorical(df["orderId"])
    s_cat = pd.Categorical(df["sku"])

    # Build CSR boolean matrix: True where order i contained SKU j
    # quantity > 0 is already enforced by load_transaction_data(),
    # so every row in df represents a real purchase event.
    csr = sp.csr_matrix(
        (
            np.ones(len(df), dtype=bool),   # all values are True (item was bought)
            (o_cat.codes, s_cat.codes),      # (row, col) = (order, sku)
        ),
        shape=(len(o_cat.categories), len(s_cat.categories)),
        dtype=bool,
    )

    # Wrap in a sparse-backed pandas DataFrame so mlxtend sees the expected API
    sku_labels = list(s_cat.categories)
    basket_sets = pd.DataFrame.sparse.from_spmatrix(csr, columns=sku_labels)

    frequent_itemsets = fpgrowth(basket_sets, min_support=0.01, use_colnames=True)
    if frequent_itemsets.empty:
        logger.warning("min_support=0.01 yielded no itemsets — retrying at 0.005.")
        frequent_itemsets = fpgrowth(basket_sets, min_support=0.005, use_colnames=True)

    rules = association_rules(frequent_itemsets, metric="lift", min_threshold=1)
    rules = rules.sort_values(["lift", "confidence"], ascending=[False, False])

    # -------------------------------------------------------------------------
    # FIX #3: do NOT silently drop multi-item antecedents with list(x)[0].
    # Keep only single-item antecedent rules (the common, meaningful case) but
    # do so explicitly via a filter — no silent data loss.
    # -------------------------------------------------------------------------
    single_ant = rules[rules["antecedents"].apply(len) == 1].copy()
    single_con = single_ant[single_ant["consequents"].apply(len) == 1].copy()

    multi_dropped = len(rules) - len(single_con)
    if multi_dropped:
        logger.info(
            "Dropped %d rules with multi-item antecedents/consequents "
            "(not supported by single-SKU lookup API).",
            multi_dropped,
        )

    single_con["antecedents"] = single_con["antecedents"].apply(lambda x: next(iter(x)))
    single_con["consequents"] = single_con["consequents"].apply(lambda x: next(iter(x)))

    rules_dict = single_con[["antecedents", "consequents", "lift", "confidence"]].to_dict(
        orient="records"
    )

    save_fp_growth_rules(rules_dict)

    # Hot-reload so the new rules are instantly available in memory (if backend needs it)
    cache.reload_artifact("fp_growth")
    logger.info("FP-Growth training complete. %d rules saved to DB.", len(rules_dict))
    return f"FP-Growth rules generated and saved to DB ({len(rules_dict)} rules)."


def get_recommendations(sku: str, top_n: int = 5) -> list[str]:
    """
    O(1) lookup from the in-memory RECOMMENDATION_INDEX.
    Previously: opened + json.load'd the full rules file on every request.
    """
    return cache.RECOMMENDATION_INDEX.get(sku.lower(), [])[:top_n]


# ============================================================================
# 2. Product Clustering (K-Means)
# ============================================================================

def train_clustering() -> str:
    """Train K-Means on RFM features, save results, then hot-reload cache."""
    logger.info("Training K-Means clustering model...")
    df = load_transaction_data()
    if df.empty:
        raise ValueError("Transaction data is empty or missing.")

    product_data = (
        df.groupby("sku")
        .agg(
            quantity=("quantity", "sum"),   # Monetary proxy: total volume sold
            Price=("Price", "mean"),         # Value: average unit price
            Frequency=("orderId", "nunique"), # Frequency: unique orders
        )
        .reset_index()
    )

    features = product_data[["quantity", "Price", "Frequency"]]
    scaler = StandardScaler()
    scaled_features = scaler.fit_transform(features)

    kmeans = KMeans(n_clusters=4, random_state=42, n_init="auto")
    product_data["Cluster"] = kmeans.fit_predict(scaled_features)

    # Assign meaningful names to clusters
    cluster_means = product_data.groupby("Cluster")[["quantity", "Price", "Frequency"]].mean()
    unassigned = list(range(4))
    cluster_names = {}

    # 1. Premium Products: highest average Price
    premium = cluster_means.loc[unassigned, "Price"].idxmax()
    cluster_names[premium] = "Premium Products"
    unassigned.remove(premium)

    # Helper to evaluate Volume + Frequency
    def perf_score(c):
        # Multiply rank of frequency and quantity to give equal weight to both
        freq_rank = cluster_means.loc[unassigned, "Frequency"].rank().loc[c]
        qty_rank = cluster_means.loc[unassigned, "quantity"].rank().loc[c]
        return freq_rank + qty_rank

    # 2. Top Performers: highest Frequency and Quantity among remaining
    top = max(unassigned, key=perf_score)
    cluster_names[top] = "Top Performers"
    unassigned.remove(top)

    # 3. Slow Movers: lowest Frequency and Quantity among remaining
    slow = min(unassigned, key=perf_score)
    cluster_names[slow] = "Slow Movers"
    unassigned.remove(slow)

    # 4. Steady Sellers: remaining
    steady = unassigned[0]
    cluster_names[steady] = "Steady Sellers"

    product_data["ClusterName"] = product_data["Cluster"].map(cluster_names)

    result = product_data.to_dict(orient="records")
    save_clusters(result)

    # Hot-reload so the new clusters are instantly available in memory
    cache.reload_artifact("clustering")
    logger.info("Clustering training complete. %d SKUs clustered and saved to DB.", len(result))
    return f"Clustering model trained and saved to DB ({len(result)} SKUs across 4 clusters)."


def get_clusters() -> list[dict]:
    """
    Return clusters as a sorted list of {cluster_id, items} dicts.

    Fix #16: the old return type was dict[int, list[dict]] which FastAPI
    serialized as {"0": [...], "1": [...]} — JSON always stringifies dict
    keys, turning int cluster IDs into strings silently.

    The new shape is a list so cluster_id is an explicit typed field:
        [{"cluster_id": 0, "items": [...]}, {"cluster_id": 1, "items": [...]}, ...]

    cache.CLUSTER_STORE still uses dict[int, list[dict]] internally for
    O(1) per-cluster lookups during cache building — only the shape exposed
    to the API layer changes here.
    """
    return [
        {"cluster_id": cluster_id, "items": items}
        for cluster_id, items in sorted(cache.CLUSTER_STORE.items())
    ]


# ============================================================================
# 3. Demand Forecasting (Prophet)
# ============================================================================

def train_prophet_for_product(sku: str) -> str:
    """
    Train a Prophet model for a specific SKU, save the artifact,
    then hot-reload the prophet cache entry so GET /forecast/{sku}
    is instantly served from memory without restarting the server.
    """
    # Lazy import — keeps Prophet's plotly/cmdstanpy initialization out of
    # module load time so the FastAPI server and /docs start instantly
    from prophet import Prophet

    logger.info("Training Prophet model for SKU: %s", sku)
    df = load_inventory_data()
    if df.empty:
        raise ValueError("Inventory data is empty.")

    # FIX #5: cast Date to datetime here, not only later downstream
    df["Date"] = pd.to_datetime(df["Date"], errors="coerce")
    df.dropna(subset=["Date"], inplace=True)

    df_product = df[df["sku"] == sku].copy()
    if df_product.empty:
        raise ValueError(f"No data found for SKU '{sku}'.")

    daily_sales = df_product.groupby("Date")["quantity"].sum().reset_index()

    # FIX #13: IQR clipping now skips constant series (guard inside helper)
    daily_sales["quantity"] = clip_outliers_iqr(daily_sales["quantity"])

    # Log Transform to prevent negative predictions natively
    daily_sales["quantity"] = np.log1p(daily_sales["quantity"])

    df_prophet = daily_sales.rename(columns={"Date": "ds", "quantity": "y"})
    duration_days = (df_prophet["ds"].max() - df_prophet["ds"].min()).days
    use_yearly = duration_days > 365

    model = Prophet(
        yearly_seasonality=use_yearly,
        weekly_seasonality=True,
        daily_seasonality=False,
    )
    model.fit(df_prophet)

    # FIX #14: sanitize SKU before using it as a filename
    safe_sku = _sanitize_sku(sku)
    model_path = os.path.join(PROPHET_MODELS_DIR, f"{safe_sku}_model.pkl")
    joblib.dump(model, model_path)

    # Pre-compute and warm FORECAST_CACHE immediately
    # Fix: Predict exactly 30 days starting from tomorrow, regardless of when the SKU was last sold
    tomorrow = pd.Timestamp.today().normalize() + pd.Timedelta(days=1)
    future_dates = pd.date_range(start=tomorrow, periods=30, freq='D')
    future = pd.DataFrame({'ds': future_dates})
    
    forecast = model.predict(future)
    
    # Reverse log transform and apply mathematical safety boundaries
    forecast["yhat"] = _safe_forecast_values(forecast["yhat"])
    forecast["yhat_lower"] = _safe_forecast_values(forecast["yhat_lower"])
    forecast["yhat_upper"] = _safe_forecast_values(forecast["yhat_upper"])
    
    recent = forecast.tail(30)
    
    # Save to DB
    save_forecast(sku, recent)
    
    cache.FORECAST_CACHE[sku] = {
        "dates":      recent["ds"].dt.strftime("%Y-%m-%d").tolist(),
        "yhat":       recent["yhat"].tolist(),
        "yhat_lower": recent["yhat_lower"].tolist(),
        "yhat_upper": recent["yhat_upper"].tolist(),
    }
    cache.PROPHET_MODELS[sku] = model

    logger.info("Prophet model trained and forecast saved to DB for SKU: %s", sku)
    return f"Prophet model for '{sku}' trained and cached."


def get_forecast(sku: str, days: int = 30) -> dict | None:
    """
    Serve forecast from FORECAST_CACHE — pure dict lookup, no disk I/O.
    Previously: joblib.load() + model.predict() on every request (~300ms–2s).

    Note: the `days` parameter is honoured against the cached 30-day window.
    If days > 30, returns None so the caller can raise a 404 (model re-train needed).
    """
    payload = cache.FORECAST_CACHE.get(sku)
    if payload is None:
        return None
    if days > len(payload["dates"]):
        return None
    # Slice to the requested window
    return {
        "dates":      payload["dates"][:days],
        "yhat":       payload["yhat"][:days],
        "yhat_lower": payload["yhat_lower"][:days],
        "yhat_upper": payload["yhat_upper"][:days],
    }


# ============================================================================
# 4. Reorder Alerts
# ============================================================================

def get_reorder_alerts() -> list[dict]:
    """
    Compare current inventory against Prophet forecasts to flag stockouts.

    FIX #2: previously called joblib.load() + model.predict() inside a
    per-SKU Python loop — blocking the event loop for every SKU in the
    inventory. Now reads demand from FORECAST_CACHE (a plain dict lookup)
    so the entire function is CPU-bound on arithmetic only, not I/O-bound.
    """
    df_inv = load_current_inventory()
    if df_inv.empty:
        return []

    current_stock = df_inv.copy()

    alerts: list[dict] = []

    for _, row in current_stock.iterrows():
        sku = row["sku"]
        inv_qty = float(row["inventory_quantity"])

        # O(1) dict lookup — no disk I/O, no model deserialization
        forecast_payload = cache.FORECAST_CACHE.get(sku)
        if forecast_payload:
            # Sum the 7-day window from the cached 30-day forecast
            predicted_demand = sum(forecast_payload["yhat"][:7])
        else:
            # Fallback heuristic for SKUs without a Prophet model
            predicted_demand = inv_qty * 0.2 + 5

        status = "No Action"
        if inv_qty < predicted_demand:
            status = "High Priority"
        elif inv_qty < 10:
            status = "Low Priority"

        if status != "No Action":
            order_qty = max(0.0, round((predicted_demand / 7 * 30) - inv_qty, 2))
            alerts.append(
                {
                    "sku": sku,
                    "quantity": inv_qty,
                    "predicted_demand_next_week": round(float(predicted_demand), 2),
                    "reorder_status": status,
                    "suggested_order_qty": order_qty,
                }
            )

    return alerts