import os
import logging
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns

from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import mean_absolute_error, mean_squared_error, silhouette_score
import scipy.sparse as sp
from mlxtend.frequent_patterns import fpgrowth, association_rules
from prophet import Prophet

from api.data_handler import load_inventory_data, load_transaction_data
from api.services import clip_outliers_iqr

# Setup logging and styling
logging.getLogger("cmdstanpy").setLevel(logging.ERROR)
logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')
sns.set_theme(style="whitegrid")

OUTPUT_DIR = "evaluation_graphs"
if not os.path.exists(OUTPUT_DIR):
    os.makedirs(OUTPUT_DIR)


def evaluate_prophet():
    logging.info("--- Evaluating Prophet (Demand Forecasting) ---")
    df = load_inventory_data()
    if df.empty:
        logging.error("No inventory data found.")
        return

    # Prepare data
    df["Date"] = pd.to_datetime(df["Date"], errors="coerce")
    df.dropna(subset=["Date"], inplace=True)
    
    # Select the top SKU by volume for evaluation
    top_sku = df.groupby("sku")["quantity"].sum().idxmax()
    logging.info(f"Selected Top SKU for evaluation: {top_sku}")
    
    df_product = df[df["sku"] == top_sku].copy()
    daily_sales = df_product.groupby("Date")["quantity"].sum().reset_index()
    daily_sales["quantity"] = clip_outliers_iqr(daily_sales["quantity"])
    
    # Train/Test Split (Last 30 days for testing)
    cutoff_date = daily_sales["Date"].max() - pd.Timedelta(days=30)
    train_data = daily_sales[daily_sales["Date"] <= cutoff_date].copy()
    test_data = daily_sales[daily_sales["Date"] > cutoff_date].copy()
    
    if len(test_data) == 0:
        logging.warning("Not enough data to split for Prophet evaluation.")
        return

    # Train Model (Log Transform)
    train_data["y"] = np.log1p(train_data["quantity"])
    train_data = train_data.rename(columns={"Date": "ds"})
    
    model = Prophet(weekly_seasonality=True, daily_seasonality=False)
    model.fit(train_data)
    
    # Predict
    future = pd.DataFrame({"ds": test_data["Date"]})
    forecast = model.predict(future)
    
    # Reverse log transform
    forecast["yhat_actual"] = np.expm1(forecast["yhat"]).clip(lower=0)
    
    # Calculate Metrics
    y_true = test_data["quantity"].values
    y_pred = forecast["yhat_actual"].values
    
    mae = mean_absolute_error(y_true, y_pred)
    rmse = np.sqrt(mean_squared_error(y_true, y_pred))
    logging.info(f"Prophet MAE:  {mae:.2f} units")
    logging.info(f"Prophet RMSE: {rmse:.2f} units")
    
    # Plotting
    plt.figure(figsize=(12, 6))
    plt.plot(train_data["ds"].tail(60), train_data["quantity"].tail(60), label='Training Data (Last 60 Days)', color='gray')
    plt.plot(test_data["Date"], y_true, label='Actual Sales (Test)', color='blue', marker='o')
    plt.plot(test_data["Date"], y_pred, label='Predicted Sales', color='red', linestyle='--', marker='x')
    
    plt.title(f'Prophet Forecast Evaluation for SKU: {top_sku}\nMAE: {mae:.2f} | RMSE: {rmse:.2f}')
    plt.xlabel('Date')
    plt.ylabel('Quantity Sold')
    plt.legend()
    plt.tight_layout()
    
    out_path = os.path.join(OUTPUT_DIR, 'prophet_evaluation.png')
    plt.savefig(out_path)
    plt.close()
    logging.info(f"Graph saved to {out_path}\n")


def evaluate_kmeans():
    logging.info("--- Evaluating K-Means (Product Clustering) ---")
    df = load_transaction_data()
    if df.empty:
        logging.error("No transaction data found.")
        return

    product_data = (
        df.groupby("sku")
        .agg(
            quantity=("quantity", "sum"),
            Price=("Price", "mean"),
            Frequency=("orderId", "nunique"),
        )
        .reset_index()
    )

    features = product_data[["quantity", "Price", "Frequency"]]
    scaler = StandardScaler()
    scaled_features = scaler.fit_transform(features)

    kmeans = KMeans(n_clusters=4, random_state=42, n_init="auto")
    labels = kmeans.fit_predict(scaled_features)
    product_data["Cluster"] = labels
    
    # Calculate Accuracy/Quality Metric
    score = silhouette_score(scaled_features, labels)
    logging.info(f"K-Means Silhouette Score: {score:.4f} (Closer to 1 is better)")
    
    # Plotting (Quantity vs Price colored by Cluster)
    plt.figure(figsize=(10, 6))
    sns.scatterplot(
        data=product_data, 
        x='quantity', 
        y='Price', 
        hue='Cluster', 
        palette='viridis', 
        alpha=0.7
    )
    plt.title(f'Product Clusters: Quantity vs Price\nSilhouette Score: {score:.4f}')
    plt.xlabel('Total Quantity Sold')
    plt.ylabel('Average Price')
    plt.xscale('log') # Log scale helps visualize highly skewed retail data
    plt.yscale('log')
    plt.tight_layout()
    
    out_path = os.path.join(OUTPUT_DIR, 'kmeans_evaluation.png')
    plt.savefig(out_path)
    plt.close()
    logging.info(f"Graph saved to {out_path}\n")


def evaluate_fpgrowth():
    logging.info("--- Evaluating FP-Growth (Market Basket Analysis) ---")
    df = load_transaction_data()
    if df.empty:
        logging.error("No transaction data found.")
        return

    top_skus = df["sku"].value_counts().head(1000).index
    df = df[df["sku"].isin(top_skus)]

    o_cat = pd.Categorical(df["orderId"])
    s_cat = pd.Categorical(df["sku"])

    csr = sp.csr_matrix(
        (np.ones(len(df), dtype=bool), (o_cat.codes, s_cat.codes)),
        shape=(len(o_cat.categories), len(s_cat.categories)),
        dtype=bool,
    )

    sku_labels = list(s_cat.categories)
    basket_sets = pd.DataFrame.sparse.from_spmatrix(csr, columns=sku_labels)

    frequent_itemsets = fpgrowth(basket_sets, min_support=0.01, use_colnames=True)
    if frequent_itemsets.empty:
        frequent_itemsets = fpgrowth(basket_sets, min_support=0.005, use_colnames=True)

    rules = association_rules(frequent_itemsets, metric="lift", min_threshold=1)
    
    logging.info(f"Found {len(rules)} association rules.")
    logging.info(f"Average Lift: {rules['lift'].mean():.2f}")
    logging.info(f"Average Confidence: {rules['confidence'].mean():.2f}")

    # Plotting Support vs Confidence
    plt.figure(figsize=(10, 6))
    scatter = plt.scatter(
        rules['support'], 
        rules['confidence'], 
        c=rules['lift'], 
        cmap='plasma', 
        alpha=0.8,
        s=rules['lift'] * 20 # Size based on lift
    )
    plt.colorbar(scatter, label='Lift')
    plt.title('FP-Growth Rules: Support vs Confidence\n(Color and Size represent Lift)')
    plt.xlabel('Support (How often items appear together)')
    plt.ylabel('Confidence (Likelihood of buying Y if X is bought)')
    plt.tight_layout()
    
    out_path = os.path.join(OUTPUT_DIR, 'fpgrowth_evaluation.png')
    plt.savefig(out_path)
    plt.close()
    logging.info(f"Graph saved to {out_path}\n")


if __name__ == "__main__":
    logging.info("Starting Model Evaluations...\n")
    evaluate_kmeans()
    evaluate_fpgrowth()
    evaluate_prophet()
    logging.info("All evaluations completed successfully!")
