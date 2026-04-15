namespace Inventory.Domain.Entities
{
    public class StockConsumption
    {
        public int Id { get; private set; }
        public int StockBatchId { get; private set; }
        public StockBatch Batch { get; private set; } = null!;
        public decimal Quantity { get; private set; }

        // Required by EF Core
        private StockConsumption() { }

        public StockConsumption(StockBatch batch, decimal quantity)
        {
            if (batch is null)
                throw new ArgumentNullException(nameof(batch));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            Batch = batch;
            StockBatchId = batch.Id;
            Quantity = quantity;
        }

        // ──────────────────────────────────────────
        // Domain Behaviour
        // ──────────────────────────────────────────

        /// <summary>
        /// Restores this consumption's quantity back to its batch.
        /// Called when an order item is removed or an order is cancelled.
        /// </summary>
        public void Rollback()
        {
            Batch.Restore(Quantity);
        }

        public void ReduceQuantity(decimal amount)
        {
            if (amount <= 0 || amount > Quantity)
                throw new ArgumentException("Invalid reduce amount.", nameof(amount));

            Quantity -= amount;
            Batch.Restore(amount);
        }
    }
}
