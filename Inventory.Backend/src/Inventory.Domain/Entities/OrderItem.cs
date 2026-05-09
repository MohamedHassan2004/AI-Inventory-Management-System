using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    
    public class OrderItem
    {
        public int Id { get; private set; }

        public int OrderId { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        
        public decimal UnitPrice { get; private set; }

        public decimal Quantity { get; private set; }
        public decimal ReturnedQuantity { get; private set; }
        public Order Order { get; private set; } = null!;
        public decimal TotalPrice => Quantity * UnitPrice;

        // Required by EF Core
        private OrderItem() { }

        
        internal OrderItem(int orderId, Product product, decimal quantity)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            OrderId = orderId;
            Product = product;
            ProductId = product.Id;
            UnitPrice = product.SellingPrice;
            Quantity = quantity;
        }

        internal void UpdateQuantity(decimal newQuantity)
        {
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(newQuantity));

            Quantity = newQuantity;
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
