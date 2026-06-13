using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    public class Order
    {
        // ─── Constants ────────────────────────────────────────────────────────────
        private const decimal DefaultTaxPercentage = 0m;
        private const decimal MaxDiscountPercentage = 70m;
        private const int DraftExpiryHours = 12;
        private const decimal DefaultDeliveryFee = 10m;

        // ─── Identity ─────────────────────────────────────────────────────────────
        public int Id { get; private set; }
        public DateTime OrderDate { get; private set; }

        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

        public DateTime? ExpiresAt { get; private set; }
        public DateTime? AllocationExpiresAt { get; private set; }

        // ─── Ownership ────────────────────────────────────────────────────────────
        public string CashierId { get; private set; } = string.Empty;
        public ApplicationUser Cashier { get; private set; } = null!;

        // ─── State ────────────────────────────────────────────────────────────────
        public OrderStatus Status { get; private set; }
        public OrderType Type { get; private set; }
        public PaymentMethod? PaymentMethod { get; private set; }

        // ─── Financials ───────────────────────────────────────────────────────────
        public decimal SubTotal { get; private set; }
        public decimal DiscountPercentage { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal DeliveryFee { get; private set; }
        public decimal TaxAmount { get; private set; }
        public decimal FinalTotal { get; private set; }

        // ─── Items ────────────────────────────────────────────────────────────────
        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        // ─── EF Core required constructor ─────────────────────────────────────────
        private Order() { }

        // ─── Legacy constructor (used by Submit) ──────────────────────────────────
        private Order(string cashierId)
        {
            CashierId = cashierId ?? throw new ArgumentNullException(nameof(cashierId));
            OrderDate = DateTime.UtcNow;
            Status = OrderStatus.Completed;
            Type = OrderType.InStore;
        }

        // =========================================================================
        //  FACTORY — Draft Workflow
        // =========================================================================

        public static Order CreateDraft(string cashierId)
        {
            if (string.IsNullOrWhiteSpace(cashierId))
                throw new ArgumentNullException(nameof(cashierId));

            return new Order
            {
                CashierId = cashierId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Draft,
                ExpiresAt = DateTime.UtcNow.AddHours(DraftExpiryHours),
                AllocationExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };
        }

        // =========================================================================
        //  DRAFT MUTATIONS
        // =========================================================================

        public void AddItem(Product product, decimal quantity)
        {
            EnsureIsDraft();

            if (product is null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing is not null)
            {
                var newQuantity = existing.Quantity + quantity;
                var extraAllocations = product.ReduceStock(quantity);
                existing.UpdateQuantity(newQuantity);
                existing.SetAllocations(extraAllocations);
            }
            else
            {
                var orderItem = new OrderItem(Id, product, quantity);
                var allocations = product.ReduceStock(quantity);
                orderItem.SetAllocations(allocations);
                _items.Add(orderItem);
            }

            AllocationExpiresAt = DateTime.UtcNow.AddMinutes(30);
            Recalculate();
        }

        public void UpdateItemQuantity(int productId, decimal quantity)
        {
            EnsureIsDraft();

            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            var existing = _items.FirstOrDefault(i => i.ProductId == productId)
                           ?? throw new InvalidOperationException($"Product {productId} is not in this order.");

            if (quantity > existing.Quantity)
            {
                var extraAllocations = existing.Product.ReduceStock(quantity - existing.Quantity);
                existing.UpdateQuantity(quantity);
                existing.SetAllocations(extraAllocations);
            }
            else if (quantity < existing.Quantity)
            {
                existing.ClearAllocations();
                var newAllocations = existing.Product.ReduceStock(quantity);
                existing.UpdateQuantity(quantity);
                existing.SetAllocations(newAllocations);
            }

            AllocationExpiresAt = DateTime.UtcNow.AddMinutes(30);
            Recalculate();
        }

        public void RemoveItem(int productId)
        {
            EnsureIsDraft();

            var item = _items.FirstOrDefault(i => i.ProductId == productId)
                       ?? throw new InvalidOperationException($"Product {productId} is not in this order.");

            item.ClearAllocations();
            _items.Remove(item);
            
            AllocationExpiresAt = DateTime.UtcNow.AddMinutes(30);
            Recalculate();
        }

        public void ApplyDiscount(decimal percentage)
        {
            if (percentage < 0 || percentage > MaxDiscountPercentage)
                throw new InvalidDiscountException(percentage);

            DiscountPercentage = percentage;
            Recalculate();
        }

        // =========================================================================
        //  CONFIRMATION
        // =========================================================================

        // Deducts stock via FEFO and creates allocations.
        // Pickup orders → Completed immediately.
        // Delivery orders → OutForDelivery (finalize with MarkAsDelivered or FailDelivery).
        public void Confirm(PaymentMethod paymentMethod, OrderType orderType)
        {
            EnsureIsDraft();

            if (!_items.Any())
                throw new EmptyOrderException();

            foreach (var item in _items)
            {
                if (!item.Allocations.Any())
                {
                    var allocations = item.Product.ReduceStock(item.Quantity);
                    item.SetAllocations(allocations);
                }
            }

            PaymentMethod = paymentMethod;
            Type = orderType;
            ExpiresAt = null;
            AllocationExpiresAt = null;

            Status = orderType == OrderType.Delivery
                ? OrderStatus.OutForDelivery
                : OrderStatus.Completed;

            Recalculate();
        }

        // =========================================================================
        //  DELIVERY WORKFLOW
        // =========================================================================

        // Marks a dispatched delivery as successfully received. Draft → OutForDelivery → Completed.
        public void MarkAsDelivered()
        {
            if (Status != OrderStatus.OutForDelivery)
                throw new InvalidOperationException(
                    $"MarkAsDelivered is only valid on OutForDelivery orders. Current status: {Status}.");

            Status = OrderStatus.Completed;
        }

        // Cancels a failed delivery and restores stock.
        // Only restores the remaining unreturned quantity per allocation (RemainingToReturn),
        // so partial returns already processed are never double-counted.
        public void FailDelivery()
        {
            if (Status != OrderStatus.OutForDelivery)
                throw new InvalidOperationException(
                    $"FailDelivery is only valid on OutForDelivery orders. Current status: {Status}.");

            foreach (var item in _items)
            {
                foreach (var allocation in item.Allocations)
                {
                    // RemainingToReturn = QuantityTaken - ReturnedQuantity
                    var toRestore = allocation.RemainingToReturn;
                    if (toRestore > 0)
                        allocation.StockBatch.Restore(toRestore);
                }
            }

            Status = OrderStatus.Cancelled;
            ExpiresAt = null;
        }

        // Cancels a Draft order (no stock was ever deducted).
        // Use FailDelivery() instead for OutForDelivery orders.
        public void Cancel()
        {
            if (Status == OrderStatus.Cancelled) return;

            if (Status != OrderStatus.Draft)
                throw new InvalidOperationException(
                    $"Cancel is only valid on Draft orders. Use FailDelivery() for OutForDelivery orders. Current status: {Status}.");

            ReleaseAllocations();

            Status = OrderStatus.Cancelled;
            ExpiresAt = null;
        }

        public void ReleaseAllocations()
        {
            if (Status != OrderStatus.Draft) return;

            foreach (var item in _items)
            {
                item.ClearAllocations();
            }

            AllocationExpiresAt = null;
        }

        // =========================================================================
        //  LEGACY — Single-Shot Submit (kept for backward compatibility)
        // =========================================================================

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
                var allocations = product.ReduceStock(quantity);
                var orderItem = new OrderItem(0, product, quantity);
                orderItem.SetAllocations(allocations);
                order._items.Add(orderItem);
            }

            order.PaymentMethod = paymentMethod;
            order.Type = orderType;
            order.ApplyDiscount(discountPercentage);
            order.Status = OrderStatus.Completed;

            return order;
        }

        // =========================================================================
        //  PRIVATE HELPERS
        // =========================================================================

        private void EnsureIsDraft()
        {
            if (Status != OrderStatus.Draft)
                throw new InvalidOperationException(
                    $"This operation is only valid on Draft orders. Current status: {Status}.");
        }

        private void Recalculate()
        {
            SubTotal = _items.Sum(i => i.TotalPrice);
            DiscountAmount = SubTotal * DiscountPercentage / 100m;

            var taxable = SubTotal - DiscountAmount;
            TaxAmount = taxable * DefaultTaxPercentage / 100m;

            DeliveryFee = Type == OrderType.Delivery ? DefaultDeliveryFee : 0m;

            FinalTotal = taxable + TaxAmount + DeliveryFee;
        }
        public void SetOrderDate(DateTime orderDate)
        {
            OrderDate = orderDate;
        }
    }
}