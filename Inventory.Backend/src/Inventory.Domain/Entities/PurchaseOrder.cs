using System;
using System.Collections.Generic;
using System.Linq;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; private set; }
        public DateTime OrderDate { get; private set; }

        public int SupplierId { get; private set; }
        public Supplier Supplier { get; private set; } = null!;

        public PurchaseOrderStatus Status { get; private set; }

        public decimal FinalTotal { get; private set; }

        private readonly List<PurchaseOrderItem> _items = new();
        public IReadOnlyCollection<PurchaseOrderItem> Items => _items.AsReadOnly();

        // Required by EF Core
        private PurchaseOrder() { }

        private PurchaseOrder(int supplierId)
        {
            if (supplierId <= 0) throw new ArgumentOutOfRangeException(nameof(supplierId));

            SupplierId = supplierId;
            OrderDate = DateTime.UtcNow;
            Status = PurchaseOrderStatus.Pending;
        }

        // ──────────────────────────────────────────
        // Factory
        // ──────────────────────────────────────────

        /// <summary>
        /// Creates a new pending purchase order for the given supplier.
        /// </summary>
        public static PurchaseOrder Create(int supplierId) => new PurchaseOrder(supplierId);

        // ──────────────────────────────────────────
        // Mutators
        // ──────────────────────────────────────────

        /// <summary>
        /// Adds a single item to the purchase order and immediately updates stock via FEFO-aware batching.
        /// Recalculates the running total after each item.
        /// </summary>
        public void AddItem(Product product, decimal quantity, decimal unitCost, DateTime expiryDate)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
            if (unitCost < 0) throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost cannot be negative.");

            product.AddStock(SupplierId, expiryDate, unitCost, quantity);

            _items.Add(new PurchaseOrderItem(product, quantity, unitCost, expiryDate));

            Recalculate();
        }

        /// <summary>
        /// Marks the order as Completed. Called once all items have been added.
        /// </summary>
        public void Complete()
        {
            if (!_items.Any())
                throw new EmptyPurchaseOrderException();

            Status = PurchaseOrderStatus.Completed;
        }

        // ──────────────────────────────────────────
        // Private helpers
        // ──────────────────────────────────────────

        private void Recalculate()
        {
            FinalTotal = _items.Sum(i => i.TotalPrice);
        }
    }
}
