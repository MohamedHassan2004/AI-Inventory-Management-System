using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    public class Order
    {
        private const decimal DefaultTaxPercentage = 14m;
        private const decimal MaxDiscountPercentage = 70m;

        public int Id { get; private set; }
        public DateTime OrderDate { get; private set; }

        public string CashierId { get; private set; } = string.Empty;
        public ApplicationUser Cashier { get; private set; } = null!;

        public OrderStatus Status { get; private set; }
        public OrderType Type { get; private set; }
        public PaymentMethod? PaymentMethod { get; private set; }

        public decimal SubTotal { get; private set; }
        public decimal DiscountPercentage { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal TaxAmount { get; private set; }
        public decimal FinalTotal { get; private set; }

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        private Order() { }

        public Order(string cashierId)
        {
            CashierId = cashierId ?? throw new ArgumentNullException(nameof(cashierId));
            OrderDate = DateTime.UtcNow;
            Status = OrderStatus.Pending;
            Type = OrderType.InStore;
        }

        private void EnsureEditable()
        {
            if (Status != OrderStatus.Pending)
                throw new OrderNotEditableException(Status);
        }

        public void AddItem(Product product, decimal quantity)
        {
            EnsureEditable();

            var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);

            if (existing != null)
                existing.AddQuantity(quantity);
            else
                _items.Add(new OrderItem(product, quantity));

            Recalculate();
        }

        public void RemoveItem(int itemId)
        {
            EnsureEditable();

            var item = _items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new OrderItemNotFoundException(itemId);

            item.Rollback();
            _items.Remove(item);

            Recalculate();
        }

        public void UpdateQuantity(int itemId, decimal quantity)
        {
            EnsureEditable();

            var item = _items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new OrderItemNotFoundException(itemId);

            item.UpdateQuantity(quantity);

            Recalculate();
        }

        public void Complete(PaymentMethod method, OrderType type)
        {
            EnsureEditable();

            if (!_items.Any())
                throw new EmptyOrderException();

            PaymentMethod = method;
            Type = type;
            Status = OrderStatus.Completed;
        }

        public void Cancel()
        {
            if (Status != OrderStatus.Pending)
                throw new OrderNotEditableException(Status);

            foreach (var item in _items)
                item.Rollback();

            Status = OrderStatus.Cancelled;
        }

        public void ApplyDiscount(decimal percentage)
        {
            if (percentage < 0 || percentage > MaxDiscountPercentage)
                throw new InvalidDiscountException(percentage);

            DiscountPercentage = percentage;
            Recalculate();
        }

        private void Recalculate()
        {
            SubTotal = _items.Sum(i => i.TotalPrice);
            DiscountAmount = SubTotal * DiscountPercentage / 100m;

            var taxable = SubTotal - DiscountAmount;
            TaxAmount = taxable * DefaultTaxPercentage / 100m;

            FinalTotal = taxable + TaxAmount;
        }
    }
}