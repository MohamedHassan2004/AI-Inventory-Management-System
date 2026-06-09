import logging
import pandas as pd
from sqlalchemy import create_engine, text

logger = logging.getLogger(__name__)

# Connection string to the SQL Server database
# For Windows Auth:
CONNECTION_STRING = "mssql+pyodbc:///?odbc_connect=Driver={ODBC Driver 17 for SQL Server};Server=.;Database=InventoryDb;Trusted_Connection=yes;"

def get_engine():
    return create_engine(CONNECTION_STRING)

def load_transaction_data() -> pd.DataFrame:
    """
    Load transaction data directly from SQL Server.
    Maps to the standardized schema: orderId, sku, quantity, Price
    """
    query = """
        SELECT o.Id as orderId, p.SKU as sku, oi.Quantity as quantity, oi.UnitPrice as Price
        FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        JOIN Products p ON oi.ProductId = p.Id
        WHERE oi.Quantity > 0
    """
    try:
        engine = get_engine()
        df = pd.read_sql(query, engine)
        
        # Enforce types
        df["orderId"] = df["orderId"].astype(int)
        df["sku"] = df["sku"].astype(str).str.strip()
        df["quantity"] = pd.to_numeric(df["quantity"], errors="coerce").fillna(0.0)
        df["Price"] = pd.to_numeric(df["Price"], errors="coerce").fillna(0.0)
        
        return df
    except Exception as exc:
        logger.error("Failed to load transaction data from DB: %s", exc)
        return pd.DataFrame()

def load_inventory_data() -> pd.DataFrame:
    """
    Load daily sales data for Prophet model training from SQL Server.
    Maps to the standardized schema: Date, sku, quantity
    """
    query = """
        SELECT CAST(o.OrderDate AS DATE) as Date, p.SKU as sku, SUM(oi.Quantity) as quantity
        FROM Orders o
        JOIN OrderItems oi ON o.Id = oi.OrderId
        JOIN Products p ON oi.ProductId = p.Id
        GROUP BY CAST(o.OrderDate AS DATE), p.SKU
    """
    try:
        engine = get_engine()
        df = pd.read_sql(query, engine)
        
        df["Date"] = pd.to_datetime(df["Date"], errors="coerce")
        df.dropna(subset=["Date"], inplace=True)
        
        df["sku"] = df["sku"].astype(str).str.strip()
        df["quantity"] = pd.to_numeric(df["quantity"], errors="coerce").fillna(0.0)
        
        return df
    except Exception as exc:
        logger.error("Failed to load inventory data from DB: %s", exc)
        return pd.DataFrame()

def load_current_inventory() -> pd.DataFrame:
    """
    Load current stock levels for each SKU directly from SQL Server.
    Returns: sku, inventory_quantity
    """
    query = """
        SELECT p.SKU as sku, ISNULL(SUM(sb.RemainingQuantity), 0) as inventory_quantity
        FROM Products p
        LEFT JOIN StockBatches sb ON p.Id = sb.ProductId
        GROUP BY p.SKU
    """
    try:
        engine = get_engine()
        df = pd.read_sql(query, engine)
        
        df["sku"] = df["sku"].astype(str).str.strip()
        df["inventory_quantity"] = pd.to_numeric(df["inventory_quantity"], errors="coerce").fillna(0.0)
        
        return df
    except Exception as exc:
        logger.error("Failed to load current inventory from DB: %s", exc)
        return pd.DataFrame()

def save_fp_growth_rules(rules_dict: list[dict]):
    engine = get_engine()
    with engine.begin() as conn:
        conn.execute(text("DELETE FROM ProductRecommendations"))
        for rule in rules_dict:
            conn.execute(
                text(f"INSERT INTO ProductRecommendations (SKU, RecommendedSKU, Score, GeneratedAt) VALUES ('{rule['antecedents']}', '{rule['consequents']}', {rule['lift']}, GETDATE())")
            )

def save_clusters(clusters_dict: list[dict]):
    engine = get_engine()
    with engine.begin() as conn:
        conn.execute(text("DELETE FROM ProductClusters"))
        for row in clusters_dict:
            conn.execute(
                text(f"INSERT INTO ProductClusters (SKU, ClusterName, GeneratedAt) VALUES ('{row['sku']}', '{row['ClusterName']}', GETDATE())")
            )

def save_forecast(sku: str, forecast_df: pd.DataFrame):
    engine = get_engine()
    with engine.begin() as conn:
        conn.execute(text(f"DELETE FROM DemandForecasts WHERE SKU = '{sku}'"))
        for _, row in forecast_df.iterrows():
            conn.execute(
                text(f"INSERT INTO DemandForecasts (SKU, ForecastDate, ForecastValue, LowerBound, UpperBound, GeneratedAt) VALUES ('{sku}', '{row['ds'].strftime('%Y-%m-%d')}', {row['yhat']}, {row['yhat_lower']}, {row['yhat_upper']}, GETDATE())")
            )