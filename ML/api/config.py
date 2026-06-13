import os

# Base directory is the parent directory of 'api'
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# ==========================================
# Data File Paths
# ==========================================
INVENTORY_FILE    = os.path.join(BASE_DIR, 'retail_store_inventory.csv')
TRANSACTION_FILE  = os.path.join(BASE_DIR, 'Assignment-1_Data.csv')

# ==========================================
# CSV Format Descriptors  (Fix #12)
# ==========================================
# Keeping separator, encoding, and engine as named constants means:
#   - data_handler.py never hardcodes format details
#   - switching datasets (e.g. semicolon-delimited export) requires
#     a one-line change here, not a hunt through parsing logic
#
# Inventory CSV: standard comma-separated UTF-8, Windows line endings (CRLF)
# — confirmed by inspection: no BOM, no mixed separators
INVENTORY_CSV = {
    "sep":      ",",          # ركز دي فاصلة عادية
    "encoding": "utf-8",
    "engine":   "c",
}

# Transaction CSV (Assignment-1_Data.csv): may carry a UTF-8 BOM and uses
# commas as separators. 'utf-8-sig' strips the BOM transparently.
# If your export is semicolon-delimited change sep here — nowhere else.
TRANSACTION_CSV = {
    "sep":      ";",          # ركز دي فاصلة منقوطة
    "encoding": "utf-8-sig",  # الـ sig بيمسح الحروف المخفية
    "engine":   "c",
}

# ==========================================
# Artifact Directories & Paths
# ==========================================
MODELS_DIR = os.path.join(BASE_DIR, 'models_data')
os.makedirs(MODELS_DIR, exist_ok=True)

FP_GROWTH_RULES_FILE    = os.path.join(MODELS_DIR, 'fp_growth_rules.json')
CLUSTERING_RESULT_FILE  = os.path.join(MODELS_DIR, 'product_clusters.json')
PROPHET_MODELS_DIR      = os.path.join(MODELS_DIR, 'prophet_models')
os.makedirs(PROPHET_MODELS_DIR, exist_ok=True)