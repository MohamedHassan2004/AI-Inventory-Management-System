using Inventory.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal SellingPrice { get; private set; }
        public int ReorderPoint { get; private set; }
        public decimal StockQuantity => Batches.Sum(b => b.RemainingQuantity);
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        private readonly List<StockBatch> _batches = new();
        public IReadOnlyCollection<StockBatch> Batches => _batches.AsReadOnly();


        public Product()
        {
        }

        public Product(string sku, string name, decimal price, int reorderPoint)
        {
            if(string.IsNullOrEmpty(sku))
                throw new ArgumentException("SKU cannot be null or empty.", nameof(sku));
            if(string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            if (price < 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Selling Price cannot be negative.");
            if(reorderPoint < 0)
                throw new ArgumentOutOfRangeException(nameof(reorderPoint), "Reorder point cannot be negative.");

            SKU = sku;
            Name = name;
            SellingPrice = price;
            ReorderPoint = reorderPoint;
        }

        #region Price
        public void UpdatePrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new ArgumentOutOfRangeException(nameof(newPrice), "Price cannot be negative.");
            SellingPrice = newPrice;
        }
        #endregion

        #region Reorder Point
        public void UpdateReorderPoint(int newReorderPoint)
        {
            if (newReorderPoint < 0)
                throw new ArgumentOutOfRangeException(nameof(newReorderPoint), "Reorder point cannot be negative.");
            ReorderPoint = newReorderPoint;
        }
        public bool NeedsReorder()
        {
            return StockQuantity <= ReorderPoint;
        }
        #endregion

        #region Quantity Management
        public void ReduceStock(decimal quantityToReduce)
        {
            if (quantityToReduce <= 0) return;

            if (quantityToReduce > StockQuantity)
                throw new InvalidOperationException($"Insufficient stock for product {Name}. Available: {StockQuantity}");

            var availableBatches = _batches
                .Where(b => b.RemainingQuantity > 0)
                .OrderBy(b => b.ExpireDate);

            foreach (var batch in availableBatches)
            {
                if (quantityToReduce <= 0) break;

                var amountFromThisBatch = Math.Min(batch.RemainingQuantity, quantityToReduce);

                batch.RemainingQuantity -= amountFromThisBatch;
                quantityToReduce -= amountFromThisBatch;
            }
        }

        public void AddStock(IDateTimeProvider dateTimeProvider,decimal quantity, decimal unitCost, DateTime expiryDate)
        {
            var batch = new StockBatch(Id, dateTimeProvider.UtcNow, expiryDate, unitCost, quantity);
            _batches.Add(batch);
        }
        #endregion
    }
}
