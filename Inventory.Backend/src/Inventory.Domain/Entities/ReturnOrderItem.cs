using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
namespace Inventory.Domain.Entities
{
    public class ReturnOrderItem
    {
        public int Id { get; private set; }

        public int ReturnOrderId { get; private set; }

        public int OriginalOrderItemId { get; private set; }
        public OrderItem OriginalOrderItem { get; private set; } = null!;

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public decimal Quantity { get; private set; }

        // price at time of original sale
        public decimal UnitPrice { get; private set; }

        public decimal RefundAmount => Quantity * UnitPrice;

        public DateTime NewExpiryDate { get; private set; }

        private ReturnOrderItem() { }

        public ReturnOrderItem(OrderItem originalItem, decimal quantity, DateTime newExpiryDate)
        {
            OriginalOrderItem = originalItem ?? throw new ArgumentNullException(nameof(originalItem));
            OriginalOrderItemId = originalItem.Id;

            Product = originalItem.Product;
            ProductId = originalItem.ProductId;

            // ── validations (all checks BEFORE mutating state) ──────

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (newExpiryDate <= DateTime.UtcNow)
                throw new ArgumentException("Expiry date must be in the future.", nameof(newExpiryDate));

            // Validate against remaining returnable quantity and update original item state
            originalItem.AddReturnedQuantity(quantity);

            // ── assignments ─────────────────────────────

            Quantity = quantity;

            // important: use original price (not current product price)
            UnitPrice = originalItem.UnitPrice;

            NewExpiryDate = newExpiryDate;
        }
    }
}