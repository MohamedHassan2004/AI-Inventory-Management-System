using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    public class ReturnOrder
    {
        public int Id { get; private set; }

        public int OriginalOrderId { get; private set; }
        public Order OriginalOrder { get; private set; } = null!;

        public string CashierId { get; private set; } = string.Empty;
        public ApplicationUser? Cashier { get; private set; }

        public DateTime ReturnDate { get; private set; }

        public string? Reason { get; private set; }

        public decimal TotalRefundAmount { get; private set; }

        private readonly List<ReturnOrderItem> _items = new();
        public IReadOnlyCollection<ReturnOrderItem> Items => _items.AsReadOnly();

        private ReturnOrder() { }

        public ReturnOrder(Order originalOrder, string cashierId, string? reason = null)
        {
            OriginalOrder = originalOrder ?? throw new ArgumentNullException(nameof(originalOrder));
            OriginalOrderId = originalOrder.Id;

            // rule: order must be completed
            if (originalOrder.Status != OrderStatus.Completed)
                throw new InvalidOperationException("Return can only be created for completed orders.");

            CashierId = cashierId ?? throw new ArgumentNullException(nameof(cashierId));
            ReturnDate = DateTime.UtcNow;
            Reason = reason;
        }

        // ──────────────────────────────────────────
        // Add item to return
        // ──────────────────────────────────────────
        public void AddItem(OrderItem originalItem, decimal quantity, DateTime expiry)
        {
            var item = new ReturnOrderItem(originalItem, quantity, expiry);

            if (_items.Any(i => i.OriginalOrderItemId == originalItem.Id))
                throw new DuplicateReturnItemException(originalItem.Id);

            _items.Add(item);
        }

        // ──────────────────────────────────────────
        // Finalize return
        // ──────────────────────────────────────────
        public void Process()
        {
            if (!_items.Any())
                throw new EmptyReturnOrderException();

            foreach (var item in _items)
            {
                var allocations = item.OriginalOrderItem.AddReturnedQuantity(item.Quantity);
                
                foreach (var alloc in allocations)
                {
                    item.Product.AddReturnedStock(
                        alloc.batch.Id,
                        alloc.batch.SupplierId, 
                        item.NewExpiryDate, 
                        alloc.batch.UnitCost, 
                        alloc.returned,
                        alloc.batch.DiscountPercentage);
                }
            }

            TotalRefundAmount = _items.Sum(i => i.RefundAmount);
        }
    }
}