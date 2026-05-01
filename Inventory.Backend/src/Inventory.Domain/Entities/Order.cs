using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    /// <summary>
    /// The Order aggregate root.
    ///
    /// Lifecycle:
    ///   CreateDraft()  →  AddItem() / RemoveItem() (repeatable)  →  Confirm()
    ///
    /// Concurrency: RowVersion is a SQL ROWVERSION token. Any stale-write attempt
    /// will cause EF Core to throw DbUpdateConcurrencyException, which callers must
    /// catch and surface to the user as a 409 Conflict.
    /// </summary>
    public class Order
    {
        // ─── Constants ────────────────────────────────────────────────────────────
        private const decimal DefaultTaxPercentage = 14m;
        private const decimal MaxDiscountPercentage = 70m;
        private const int DraftExpiryHours = 12;

        // ─── Identity ─────────────────────────────────────────────────────────────
        public int Id { get; private set; }
        public DateTime OrderDate { get; private set; }

        /// <summary>
        /// Optimistic concurrency token. EF Core maps this to a SQL ROWVERSION
        /// column that is auto-incremented by the DB on every UPDATE.
        /// </summary>
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

        /// <summary>
        /// Drafts expire after <see cref="DraftExpiryHours"/> hours.
        /// Null for confirmed/cancelled orders.
        /// </summary>
        public DateTime? ExpiresAt { get; private set; }

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
            Status = OrderStatus.Pending;
            Type = OrderType.InStore;
        }

        // =========================================================================
        //  FACTORY — Draft Workflow
        // =========================================================================

        /// <summary>
        /// Creates a new empty Draft order. No items are added yet.
        /// The order expires in <see cref="DraftExpiryHours"/> hours.
        /// </summary>
        public static Order CreateDraft(string cashierId, OrderType orderType)
        {
            if (string.IsNullOrWhiteSpace(cashierId))
                throw new ArgumentNullException(nameof(cashierId));

            return new Order
            {
                CashierId = cashierId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Draft,
                Type = orderType,
                ExpiresAt = DateTime.UtcNow.AddHours(DraftExpiryHours)
            };
        }

        // =========================================================================
        //  DRAFT MUTATIONS
        // =========================================================================

        /// <summary>
        /// Adds a product to the draft or updates its quantity if it already exists.
        /// Price is snapshotted from Product.SellingPrice at this moment (backend SSOT).
        /// No stock deduction occurs here — that is deferred to Confirm().
        /// </summary>
        public void AddItem(Product product, decimal quantity, decimal discountPercentage = 0)
        {
            EnsureIsDraft();

            if (product is null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing is not null)
            {
                existing.UpdateQuantity(quantity, product.SellingPrice);
            }
            else
            {
                _items.Add(new OrderItem(Id, product, quantity));
            }

            ApplyDiscount(discountPercentage);
        }

        /// <summary>
        /// Removes a line item from the draft by product ID.
        /// </summary>
        public void RemoveItem(int productId)
        {
            EnsureIsDraft();

            var item = _items.FirstOrDefault(i => i.ProductId == productId)
                       ?? throw new InvalidOperationException($"Product {productId} is not in this order.");

            _items.Remove(item);
            Recalculate();
        }

        /// <summary>
        /// Updates the order-level discount and recalculates all totals.
        /// </summary>
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

        /// <summary>
        /// Transitions the Draft to Completed.
        /// This is the ONLY point where stock is permanently deducted.
        /// Must be called inside a database transaction.
        /// </summary>
        public void Confirm(PaymentMethod paymentMethod)
        {
            EnsureIsDraft();

            if (!_items.Any())
                throw new EmptyOrderException();

            // Permanently deduct stock via FEFO on each product.
            // Each Product must have been loaded with its Batches collection.
            foreach (var item in _items)
            {
                item.Product.ReduceStock(item.Quantity);
            }

            PaymentMethod = paymentMethod;
            Status = OrderStatus.Completed;
            ExpiresAt = null; // no longer a draft
        }

        /// <summary>
        /// Cancels the draft. Idempotent if already cancelled.
        /// </summary>
        public void Cancel()
        {
            if (Status == OrderStatus.Cancelled) return;

            if (Status == OrderStatus.Completed)
                throw new InvalidOperationException("A completed order cannot be cancelled through this workflow.");

            Status = OrderStatus.Cancelled;
            ExpiresAt = null;
        }

        // =========================================================================
        //  LEGACY — Single-Shot Submit (kept for backward compatibility)
        // =========================================================================

        /// <summary>
        /// Original "God Method" — creates and immediately completes an order in one shot.
        /// Kept for backward compatibility with the old frontend submission workflow.
        /// </summary>
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
                product.ReduceStock(quantity);
                order._items.Add(new OrderItem(0, product, quantity));
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

            FinalTotal = taxable + TaxAmount;
        }
    }
}