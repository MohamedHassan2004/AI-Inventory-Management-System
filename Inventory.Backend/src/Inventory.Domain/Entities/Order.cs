using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

    namespace Inventory.Domain.Entities
    {
        public class Order
        {
            public int Id { get; private set; }
            public DateTime OrderDate { get; private set; }
            public string CashierId { get; private set; }
            public virtual ApplicationUser Cashier { get; private set; }

            public OrderStatus Status { get; private set; }
            public OrderType Type { get; private set; }
            public PaymentMethod? PaymentMethod { get; private set; }

            public decimal SubTotal { get; private set; } = 0;
            public decimal DiscountPercentage { get; private set; } = 0;
            public decimal DiscountAmount { get; private set; }
            public decimal TaxAmount { get; private set; }
            public decimal FinalTotal { get; private set; }

            private readonly List<OrderItem> _items = new();
            public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

            private Order() { }

            public Order(string cashierId)
            {
                if (string.IsNullOrWhiteSpace(cashierId))
                    throw new ArgumentException("CashierId is required");

                OrderDate = DateTime.UtcNow;
                CashierId = cashierId;
                Status = OrderStatus.Pending;
                Type = OrderType.InStore;
            }

            #region Guards

            private void EnsureNotCompletedOrCancelled()
            {
                if (Status == OrderStatus.Completed)
                    throw new InvalidOperationException("Order is already completed");

                if (Status == OrderStatus.Cancelled)
                    throw new InvalidOperationException("Order is cancelled");
            }

        #endregion


        #region Items

        public void AddItem(OrderItem item)
        {
            EnsureNotCompletedOrCancelled();

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var existingItem = _items.FirstOrDefault(i => i.ProductId == item.ProductId);

            if (existingItem != null)
            {
                existingItem.AddQuantity(item.Quantity);
            }
            else
            {
                _items.Add(item);
            }

            RecalculateTotals();
        }

        public void RemoveItem(OrderItem item)
        {
            EnsureNotCompletedOrCancelled();

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            item.Remove();

            if (_items.Remove(item))
            {
                RecalculateTotals();
            }
        }

        public void RemoveItem(int itemId)
        {
            EnsureNotCompletedOrCancelled();

            var item = _items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
                throw new InvalidOperationException("Item not found");

            // reuse method علشان نحافظ على DRY
            RemoveItem(item);
        }

        public void UpdateItemQuantity(int itemId, decimal newQuantity)
        {
            EnsureNotCompletedOrCancelled();

            var item = _items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
                throw new InvalidOperationException("Item not found");

            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero");

            // رجوع الكمية القديمة
            item.Remove();

            // إضافة الكمية الجديدة
            item.AddQuantity(newQuantity);

            RecalculateTotals();
        }

        #endregion


        #region Order Actions

        public void CompleteOrder(PaymentMethod paymentMethod, OrderType orderType)
            {
                EnsureNotCompletedOrCancelled();

                if (!_items.Any())
                    throw new InvalidOperationException("Cannot complete an order with no items.");

                PaymentMethod = paymentMethod;
                Type = orderType;
                Status = OrderStatus.Completed;
            }

            public void Cancel()
            {
                if (Status == OrderStatus.Completed)
                    throw new InvalidOperationException("Cannot cancel completed order");

                if (Status == OrderStatus.Cancelled)
                    return;

                foreach (var item in _items)
                {
                    item.Remove(); // رجوع stock
                }

                Status = OrderStatus.Cancelled;
            }

            #endregion

            #region Financial Calculations

            public void ApplyDiscount(decimal discountPercentage)
            {
                EnsureNotCompletedOrCancelled();

                if (discountPercentage < 0 || discountPercentage > 70)
                    throw new ArgumentException("Invalid discount percentage");

                DiscountPercentage = discountPercentage;

                // 🔥 الحساب كله هنا فقط
                RecalculateTotals();
            }

            public void CalculateTax(decimal taxPercentage = 14)
            {
                TaxAmount = (SubTotal - DiscountAmount) * (taxPercentage / 100);
            }

            private void RecalculateTotals()
            {
                SubTotal = _items.Sum(i => i.TotalPrice);

                // 🔥 Single Source of Truth
                DiscountAmount = DiscountPercentage / 100 * SubTotal;

                CalculateTax();

                FinalTotal = SubTotal - DiscountAmount + TaxAmount;
            }

            #endregion
        }
    }
