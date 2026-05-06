using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public decimal UnitPrice { get; private set; }

        public decimal Quantity { get; private set; }
        public decimal ReturnedQuantity { get; private set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        private OrderItem() { }

        public OrderItem(Product product, decimal quantity)
        {
            Product = product ?? throw new ArgumentNullException(nameof(product));
            ProductId = product.Id;
            UnitPrice = product.SellingPrice;
            if (product.StockQuantity < quantity)
                throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);
            Quantity = quantity;
        }

        public void AddReturnedQuantity(decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (ReturnedQuantity + quantity > Quantity)
                throw new ReturnQuantityExceededException(Id, quantity, Quantity - ReturnedQuantity);

            ReturnedQuantity += quantity;
        }
    }
}
