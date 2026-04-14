namespace Inventory.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public decimal UnitPrice { get; private set; }

        public decimal Quantity => _consumptions.Sum(c => c.Quantity);
        public decimal TotalPrice => Quantity * UnitPrice;

        private readonly List<StockConsumption> _consumptions = new();
        public IReadOnlyCollection<StockConsumption> Consumptions => _consumptions.AsReadOnly();

        private OrderItem() { }

        public OrderItem(Product product, decimal quantity)
        {
            Product = product ?? throw new ArgumentNullException(nameof(product));
            ProductId = product.Id;
            UnitPrice = product.SellingPrice;

            AddQuantity(quantity);
        }

        public void AddQuantity(decimal quantity)
        {
            var consumptions = Product.ReduceStock(quantity);
            _consumptions.AddRange(consumptions);
        }

        public void UpdateQuantity(decimal newQuantity)
        {
            var delta = newQuantity - Quantity;

            if (delta > 0)
                AddQuantity(delta);
            else if (delta < 0)
                RollbackExcess(-delta);
        }

        public void Rollback()
        {
            foreach (var c in _consumptions)
                c.Rollback();

            _consumptions.Clear();
        }

        private void RollbackExcess(decimal quantity)
        {
            for (int i = _consumptions.Count - 1; i >= 0 && quantity > 0; i--)
            {
                var c = _consumptions[i];
                var take = Math.Min(c.Quantity, quantity);

                c.Batch.Restore(take);
                quantity -= take;

                if (take == c.Quantity)
                    _consumptions.RemoveAt(i);
            }
        }
    }
}
