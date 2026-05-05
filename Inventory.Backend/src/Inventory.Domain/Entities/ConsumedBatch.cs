namespace Inventory.Domain.Entities
{
    /// <summary>
    /// A simple data carrier used by the Product aggregate to report back
    /// which batches were consumed and in what quantity.
    /// </summary>
    public class ConsumedBatch
    {
        public int StockBatchId { get; }
        public decimal Quantity { get; }

        public ConsumedBatch(int stockBatchId, decimal quantity)
        {
            if (stockBatchId <= 0)
                throw new ArgumentOutOfRangeException(nameof(stockBatchId), "Stock batch ID must be greater than zero.");

            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

            StockBatchId = stockBatchId;
            Quantity = quantity;
        }
    }
}
