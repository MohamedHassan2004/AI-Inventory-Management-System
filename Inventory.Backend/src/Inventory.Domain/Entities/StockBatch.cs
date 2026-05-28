namespace Inventory.Domain.Entities
{
    public class StockBatch
    {
        public int Id { get; private set; }
        public DateTime PurchaseDate { get; private set; }
        public DateTime ExpireDate { get; private set; }
        public decimal UnitCost { get; private set; }
        public decimal DiscountPercentage { get; private set; }
        public decimal OriginalQuantity { get; private set; }
        public decimal RemainingQuantity { get; private set; }

        public int SupplierId { get; private set; }
        public Supplier Supplier { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public ICollection<OrderItemBatchAllocation> Allocations { get; private set; } = new List<OrderItemBatchAllocation>();

        // Required by EF Core
        private StockBatch() { }

        public StockBatch(int productId, int supplierId, DateTime expireDate, decimal unitCost, decimal quantity, decimal discountPercentage = 0)
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

            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentOutOfRangeException(nameof(discountPercentage), "Discount percentage must be between 0 and 100.");

            ProductId = productId;
            SupplierId = supplierId;
            PurchaseDate = DateTime.UtcNow;
            ExpireDate = expireDate;
            UnitCost = unitCost;
            DiscountPercentage = discountPercentage;
            OriginalQuantity = quantity;
            RemainingQuantity = quantity;
        }

        // ──────────────────────────────────────────
        // Domain Behaviour
        // ──────────────────────────────────────────

        
        public void Consume(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Consumed amount must be greater than zero.", nameof(amount));

            if (amount > RemainingQuantity)
                throw new InvalidOperationException(
                    $"Cannot consume {amount} from batch {Id}. Only {RemainingQuantity} remaining.");

            RemainingQuantity -= amount;
        }

        
        public void Restore(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Restored amount must be greater than zero.", nameof(amount));

            if (RemainingQuantity + amount > OriginalQuantity)
                throw new InvalidOperationException(
                    $"Cannot restore {amount} to batch {Id}. Would exceed original quantity of {OriginalQuantity}.");

            RemainingQuantity += amount;
        }

        public void UpdateBatch(DateTime expireDate, decimal unitCost, decimal remainingQuantity, decimal discountPercentage = 0)
        {
            if (expireDate <= PurchaseDate)
                throw new ArgumentException("Expire date must be after purchase date.", nameof(expireDate));
            if (unitCost < 0)
                throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");
            if (remainingQuantity < 0 || remainingQuantity > OriginalQuantity)
                throw new ArgumentOutOfRangeException(nameof(remainingQuantity), "Remaining quantity must be between 0 and original quantity.");
            if (discountPercentage < 0 || discountPercentage > 100)
                throw new ArgumentOutOfRangeException(nameof(discountPercentage), "Discount percentage must be between 0 and 100.");

            ExpireDate = expireDate;
            UnitCost = unitCost;
            RemainingQuantity = remainingQuantity;
            DiscountPercentage = discountPercentage;
        }

        public bool HasStock => RemainingQuantity > 0;
        public bool IsExpired => ExpireDate <= DateTime.UtcNow;
    }
}
