using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Entities
{
    public class StockBatch
    {
        public int Id { get; set; }
        public DateTime PurchaseDate { get; private set; }
        public DateTime ExpireDate { get; private set; }
        public decimal UnitCost { get; private set; }
        public decimal OriginalQuantity { get; private set; }
        public decimal RemainingQuantity { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;


        public StockBatch()
        {
        }

        public StockBatch(int productId, DateTime purchaseDate, DateTime expireDate, decimal unitCost, decimal quantity)
        {
            if (productId < 0)
                throw new ArgumentOutOfRangeException(nameof(productId), "Product ID cannot be negative.");
            if (expireDate <= purchaseDate)
                throw new ArgumentException("Expire date must be after purchase date.", nameof(expireDate));
            if (unitCost < 0)
                throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");
            if (quantity < 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity cannot be negative.");

            ProductId = productId;
            PurchaseDate = purchaseDate;
            ExpireDate = expireDate;
            UnitCost = unitCost;
            OriginalQuantity = quantity;
            RemainingQuantity = quantity;
        }

        public void UpdateBatch(DateTime expireDate, decimal unitCost, decimal remainingQuantity)
        {
            if (expireDate <= PurchaseDate)
                throw new ArgumentException("Expire date must be after purchase date.", nameof(expireDate));
            if (unitCost < 0)
                throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");
            if (remainingQuantity < 0 || remainingQuantity > OriginalQuantity)
                throw new ArgumentOutOfRangeException(nameof(remainingQuantity), "Remaining quantity must be between 0 and original quantity.");
            ExpireDate = expireDate;
            UnitCost = unitCost;
            RemainingQuantity = remainingQuantity;
        }

    }
}
