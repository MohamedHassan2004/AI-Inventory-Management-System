using System;

namespace Inventory.Domain.Entities
{
    public class OrderItemBatchAllocation
    {
        public int Id { get; private set; }

        public int OrderItemId { get; private set; }
        public OrderItem OrderItem { get; private set; } = null!;

        public int StockBatchId { get; private set; }
        public StockBatch StockBatch { get; private set; } = null!;

        public decimal QuantityTaken { get; private set; }
        public decimal ReturnedQuantity { get; private set; }

        public decimal UnitPrice { get; private set; }
        public decimal DiscountPercentage { get; private set; }

        public decimal RemainingToReturn => QuantityTaken - ReturnedQuantity;

        private OrderItemBatchAllocation() { }

        internal OrderItemBatchAllocation(int stockBatchId, decimal quantityTaken, decimal unitPrice, decimal discountPercentage)
        {
            if (stockBatchId < 0) throw new ArgumentOutOfRangeException(nameof(stockBatchId));
            if (quantityTaken <= 0) throw new ArgumentOutOfRangeException(nameof(quantityTaken));
            if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
            if (discountPercentage < 0 || discountPercentage > 100) throw new ArgumentOutOfRangeException(nameof(discountPercentage));

            StockBatchId = stockBatchId;
            QuantityTaken = quantityTaken;
            UnitPrice = unitPrice;
            DiscountPercentage = discountPercentage;
            ReturnedQuantity = 0;
        }

        internal void AddReturn(decimal quantity)
        {
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (ReturnedQuantity + quantity > QuantityTaken)
                throw new InvalidOperationException($"Cannot return {quantity}. Only {RemainingToReturn} left to return for this allocation.");

            ReturnedQuantity += quantity;
        }

        // Sets the StockBatch navigation reference — used by SetAllocations so FailDelivery
        // can call batch.Restore() in-memory without relying on EF Core lazy-loading.
        internal void SetStockBatch(StockBatch batch) => StockBatch = batch;
    }
}
