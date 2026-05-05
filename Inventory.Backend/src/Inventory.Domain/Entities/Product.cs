using Inventory.Domain.Interfaces;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    public class Product
    {
        public int Id { get; private set; }
        public string SKU { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public decimal SellingPrice { get; private set; }
        public int ReorderPoint { get; private set; }

        public int? CategoryId { get; private set; }
        public Category? Category { get; private set; }

        public decimal StockQuantity => _batches.Sum(b => b.RemainingQuantity);

        private readonly List<StockBatch> _batches = new();
        public IReadOnlyCollection<StockBatch> Batches => _batches.AsReadOnly();

        // Required by EF Core
        private Product() { }

        public Product(string sku, string name, decimal sellingPrice, int reorderPoint)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU cannot be null or empty.", nameof(sku));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            if (sellingPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(sellingPrice), "Selling price cannot be negative.");

            if (reorderPoint < 0)
                throw new ArgumentOutOfRangeException(nameof(reorderPoint), "Reorder point cannot be negative.");

            SKU = sku;
            Name = name;
            SellingPrice = sellingPrice;
            ReorderPoint = reorderPoint;
        }

        // ──────────────────────────────────────────
        // Identity & Classification
        // ──────────────────────────────────────────

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("Name cannot be null or empty.", nameof(newName));

            Name = newName;
        }

        public void AssignCategory(int? categoryId)
        {
            if (categoryId <= 0)
                throw new ArgumentOutOfRangeException(nameof(categoryId), "Category ID must be greater than zero.");

            CategoryId = categoryId;
        }

        public void RemoveCategory() => CategoryId = null;

        // ──────────────────────────────────────────
        // Pricing
        // ──────────────────────────────────────────

        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(newPrice), "Selling price cannot be negative.");

            SellingPrice = newPrice;
        }

        // ──────────────────────────────────────────
        // Reorder Point
        // ──────────────────────────────────────────

        public void UpdateReorderPoint(int newReorderPoint)
        {
            if (newReorderPoint < 0)
                throw new ArgumentOutOfRangeException(nameof(newReorderPoint), "Reorder point cannot be negative.");

            ReorderPoint = newReorderPoint;
        }

        public bool NeedsReorder() => StockQuantity <= ReorderPoint;

        // ──────────────────────────────────────────
        // Stock Management
        // ──────────────────────────────────────────

        /// <summary>
        /// Adds a new stock batch for this product (FEFO-aware).
        /// </summary>
        public void AddStock(int supplierId, DateTime expiryDate, decimal unitCost, decimal quantity)
        {
            var batch = new StockBatch(Id, supplierId, expiryDate, unitCost, quantity);
            _batches.Add(batch);
        }

        /// <summary>
        /// Reduces stock using FIFO (First In First Out) strategy based on PurchaseDate.
        /// </summary>
        /// <returns>A list of consumed batch information for traceability.</returns>
        public IReadOnlyList<ConsumedBatch> ReduceStock(decimal quantityToReduce)
        {
            if (quantityToReduce <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantityToReduce));

            if (quantityToReduce > StockQuantity)
                throw new InsufficientStockException(Name, quantityToReduce, StockQuantity);

            var consumptions = new List<ConsumedBatch>();

            var availableBatches = _batches
                .Where(b => b.HasStock && !b.IsExpired)
                .OrderBy(b => b.PurchaseDate)
                .ThenBy(b => b.Id);

            foreach (var batch in availableBatches)
            {
                if (quantityToReduce <= 0) break;

                var taken = Math.Min(batch.RemainingQuantity, quantityToReduce);

                batch.Consume(taken);
                consumptions.Add(new ConsumedBatch(batch.Id, taken));
                
                quantityToReduce -= taken;
            }

            // Fallback: if expired batches needed to fulfill (shouldn't happen in healthy stock)
            if (quantityToReduce > 0)
                throw new InsufficientStockException(Name, quantityToReduce, 0);

            return consumptions.AsReadOnly();
        }
    }
}
