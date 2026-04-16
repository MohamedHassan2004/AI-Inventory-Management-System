namespace Inventory.Domain.Entities
{
    public class StockBatch
    {
        public int Id { get; private set; }
        public DateTime PurchaseDate { get; private set; }
        public DateTime ExpireDate { get; private set; }
        public decimal UnitCost { get; private set; }
        public decimal OriginalQuantity { get; private set; }
        public decimal RemainingQuantity { get; private set; }

        public int SupplierId { get; private set; }
        public Supplier Supplier { get; private set; } = null!;

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        // Required by EF Core
        private StockBatch() { }

        public StockBatch(int productId, int supplierId, DateTime expireDate, decimal unitCost, decimal quantity)
        {
            if (productId <= 0)
                throw new ArgumentOutOfRangeException(nameof(productId), "Product ID must be greater than zero.");

            if (supplierId <= 0)
                throw new ArgumentOutOfRangeException(nameof(supplierId), "Supplier ID must be greater than zero.");

            if (expireDate <= DateTime.UtcNow)
                throw new ArgumentException("Expire date must be after purchase date.", nameof(expireDate));

            if (unitCost < 0)
                throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");

            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

            ProductId = productId;
            SupplierId = supplierId;
            PurchaseDate = DateTime.UtcNow;
            ExpireDate = expireDate;
            UnitCost = unitCost;
            OriginalQuantity = quantity;
            RemainingQuantity = quantity;
        }

        // ──────────────────────────────────────────
        // Domain Behaviour
        // ──────────────────────────────────────────

        /// <summary>
        /// Reduces the remaining quantity of this batch (used during stock consumption).
        /// </summary>
        public void Consume(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Consumed amount must be greater than zero.", nameof(amount));

            if (amount > RemainingQuantity)
                throw new InvalidOperationException(
                    $"Cannot consume {amount} from batch {Id}. Only {RemainingQuantity} remaining.");

            RemainingQuantity -= amount;
        }

        /// <summary>
        /// Restores quantity back to this batch (used during order cancellation or item removal).
        /// </summary>
        public void Restore(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Restored amount must be greater than zero.", nameof(amount));

            if (RemainingQuantity + amount > OriginalQuantity)
                throw new InvalidOperationException(
                    $"Cannot restore {amount} to batch {Id}. Would exceed original quantity of {OriginalQuantity}.");

            RemainingQuantity += amount;
        }

        public void UpdateBatch(DateTime expireDate, decimal unitCost, decimal remainingQuantity)
        {
            if (expireDate <= PurchaseDate)
                throw new ArgumentException("Expire date must be after purchase date.", nameof(expireDate));
            if (unitCost < 0)
                throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");
            if (remainingQuantity < 0 || remainingQuantity > OriginalQuantity)
                throw new ArgumentOutOfRangeException(nameof(remainingQuantity), "Remaining quantity must be between 0 and original quantity.");
            ExpireDate = expireDate;
            UnitCost = unitCost;
            RemainingQuantity = remainingQuantity;
        }

        public bool HasStock => RemainingQuantity > 0;
        public bool IsExpired => ExpireDate <= DateTime.UtcNow;
    }
}
