using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; }

        public decimal Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public int OrderId { get; private set; }
        public virtual Order Order { get; private set; }

        public decimal TotalPrice => Quantity * UnitPrice;

        private readonly List<StockConsumption> _consumptions = new();
        public IReadOnlyCollection<StockConsumption> Consumptions => _consumptions.AsReadOnly();

        private OrderItem() { }

        public OrderItem(Product product, decimal quantity)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero");

            Product = product;
            ProductId = product.Id;
            UnitPrice = product.SellingPrice;

            AddQuantity(quantity);
        }

        public void AddQuantity(decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero");

            if (Product == null)
                throw new InvalidOperationException("Product must be loaded");

            var consumptions = Product.ReduceStock(quantity);
            _consumptions.AddRange(consumptions);
            Quantity += quantity;
        }
        public void Remove()
        {
            if (!_consumptions.Any())
                return;

            foreach (var c in _consumptions)
            {
                c.Batch.RemainingQuantity += c.Quantity;
            }

            _consumptions.Clear();
            Quantity = 0;
        }
    }
}
