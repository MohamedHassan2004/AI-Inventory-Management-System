using System;

namespace Inventory.Domain.Entities
{
    public class PurchaseOrderItem
    {
        public int Id { get; private set; }
        
        public int PurchaseOrderId { get; private set; }
        public PurchaseOrder PurchaseOrder { get; private set; } = null!;
        
        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public decimal Quantity { get; private set; }
        public decimal UnitCost { get; private set; }
        public DateTime ExpiryDate { get; private set; }

        public decimal DiscountPercentage { get; private set; } = 0;  

        public decimal TotalPrice => Quantity * UnitCost;

        private PurchaseOrderItem() { }

        internal PurchaseOrderItem(Product product, decimal quantity, decimal unitCost, DateTime expiryDate, decimal discountPercentage = 0)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
            if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost));
            
            ProductId = product.Id;
            Product = product;
            Quantity = quantity;
            UnitCost = unitCost;
            ExpiryDate = expiryDate;
            DiscountPercentage = discountPercentage;
        }
    }
}
