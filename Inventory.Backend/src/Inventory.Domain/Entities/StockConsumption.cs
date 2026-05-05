using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    public class StockConsumption
    {
        public int Id { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public int StockBatchId { get; private set; }
        public StockBatch StockBatch { get; private set; } = null!;

        public int OrderItemId { get; private set; }
        public OrderItem OrderItem { get; private set; } = null!;

        public decimal Quantity { get; private set; }
        public decimal ReturnedQuantity { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public decimal RemainingToReturn => Quantity - ReturnedQuantity;

        // Required by EF Core
        private StockConsumption() { }

        public StockConsumption(int productId, int stockBatchId, decimal quantity)
        {
            if (productId <= 0)
                throw new ArgumentOutOfRangeException(nameof(productId), "Product ID must be greater than zero.");

            if (stockBatchId <= 0)
                throw new ArgumentOutOfRangeException(nameof(stockBatchId), "Stock batch ID must be greater than zero.");

            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

            ProductId = productId;
            StockBatchId = stockBatchId;
            Quantity = quantity;
            CreatedAt = DateTime.UtcNow;
            ReturnedQuantity = 0;
        }

        internal void SetOrderItem(OrderItem orderItem)
        {
            OrderItem = orderItem ?? throw new ArgumentNullException(nameof(orderItem));
            // We rely on EF Core to synchronize OrderItemId when the graph is saved.
        }

        public void Return(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Return amount must be greater than zero.", nameof(amount));

            if (amount > RemainingToReturn)
                throw new InvalidOperationException($"Cannot return {amount} units. Only {RemainingToReturn} units available from this consumption.");

            ReturnedQuantity += amount;
        }
    }
}
