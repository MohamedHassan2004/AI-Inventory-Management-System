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

        private Order(string cashierId)
        {
            CashierId = cashierId ?? throw new ArgumentNullException(nameof(cashierId));
            OrderDate = DateTime.UtcNow;
            Status = OrderStatus.Pending;
            Type = OrderType.InStore;
        }

        public static Order Submit(
            string cashierId,
            IReadOnlyList<(Product product, decimal quantity)> items,
            PaymentMethod paymentMethod,
            OrderType orderType,
            decimal discountPercentage)
        {
            if (items == null || !items.Any())
                throw new EmptyOrderException();

            var order = new Order(cashierId);

            foreach (var (product, quantity) in items)
            {
                var consumedBatches = product.ReduceStock(quantity);
                var orderItem = new OrderItem(product, quantity);
                
                foreach (var consumed in consumedBatches)
                {
                    orderItem.AddStockConsumption(new StockConsumption(product.Id, consumed.StockBatchId, consumed.Quantity));
                }

                orderItem.ValidateConsumptions();
                order._items.Add(orderItem);
            }

            order.PaymentMethod = paymentMethod;
            order.Type = orderType;
            order.ApplyDiscount(discountPercentage);
            order.Status = OrderStatus.Completed;

            return order;
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