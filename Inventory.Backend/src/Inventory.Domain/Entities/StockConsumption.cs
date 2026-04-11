namespace Inventory.Domain.Entities
{
    public class StockConsumption
    {
        public int Id { get; private set; }
        public int StockBatchId { get; private set; }
        public StockBatch Batch { get; private set; }

        public decimal Quantity { get; private set; }

        private StockConsumption() { }

        public StockConsumption(StockBatch batch, decimal quantity)
        {
            Batch = batch ?? throw new ArgumentNullException(nameof(batch));
            StockBatchId = batch.Id;
            Quantity = quantity;
        }

        public void Decrease(decimal amount)
        {
            if (amount > Quantity)
                throw new InvalidOperationException();

            Quantity -= amount;
        }
    }
}