using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    /// <summary>
    /// A line item within an Order. UnitPrice is snapshot-locked at the time
    /// the item is added to the Draft (backend single source of truth).
    /// No stock deduction happens here — that is deferred to Order.Confirm().
    /// </summary>
    public class OrderItem
    {
        public int Id { get; private set; }

        public int OrderId { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        /// <summary>Price snapshotted from Product.SellingPrice at the time of AddItem.</summary>
        public decimal UnitPrice { get; private set; }

        public decimal Quantity { get; private set; }

        /// <summary>Computed: UnitPrice × Quantity. Not persisted (EF Ignored).</summary>
        public decimal TotalPrice => Quantity * UnitPrice;

        // Required by EF Core
        private OrderItem() { }

        /// <summary>
        /// Used by Order.AddItem() when creating a brand-new line item.
        /// Stock check is intentionally deferred to Order.Confirm().
        /// </summary>
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

        /// <summary>
        /// Updates the quantity on an existing draft line item.
        /// Price is re-snapshotted from the current product price.
        /// </summary>
        internal void UpdateQuantity(decimal newQuantity, decimal currentUnitPrice)
        {
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(newQuantity));

            Quantity = newQuantity;
            UnitPrice = currentUnitPrice; // re-snapshot price in case it changed
        }
    }
}
