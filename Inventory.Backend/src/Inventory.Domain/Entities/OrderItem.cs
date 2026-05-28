using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities
{
    
    public class OrderItem
    {
        public int Id { get; private set; }

        public int OrderId { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        
        public decimal UnitPrice { get; private set; }

        public decimal Quantity { get; private set; }
        public decimal ReturnedQuantity { get; private set; }
        public Order Order { get; private set; } = null!;
        
        public decimal TotalPrice => Allocations.Any()
            ? Allocations.Sum(a => a.QuantityTaken * Product.SellingPrice * (1 - a.DiscountPercentage / 100m))
            : CalculateEstimatedPrice();

        private readonly List<OrderItemBatchAllocation> _allocations = new();
        public IReadOnlyCollection<OrderItemBatchAllocation> Allocations => _allocations.AsReadOnly();

        // Required by EF Core
        private OrderItem() { }

        
        internal OrderItem(int orderId, Product product, decimal quantity)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if(product.StockQuantity < quantity)
                throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);

            OrderId = orderId;
            Product = product;
            ProductId = product.Id;
            UnitPrice = product.SellingPrice;
            Quantity = quantity;
        }

        internal void UpdateQuantity(decimal newQuantity)
        {
            if (newQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(newQuantity));

            Quantity = newQuantity;
        }

        internal void SetAllocations(List<(StockBatch batch, decimal taken)> allocations)
        {
            foreach (var a in allocations)
            {
                var alloc = new OrderItemBatchAllocation(a.batch.Id, a.taken, a.batch.Product.SellingPrice, a.batch.DiscountPercentage);
                alloc.SetStockBatch(a.batch);
                _allocations.Add(alloc);
            }
        }

        private decimal CalculateEstimatedPrice()
        {
            if (Product == null || Product.Batches == null || !Product.Batches.Any())
            {
                return Quantity * UnitPrice;
            }

            var remaining = Quantity;
            decimal total = 0;

            var availableBatches = Product.Batches
                .Where(b => b.HasStock && !b.IsExpired)
                .OrderBy(b => b.ExpireDate)
                .ThenBy(b => b.PurchaseDate)
                .ThenBy(b => b.Id)
                .ToList();

            foreach (var batch in availableBatches)
            {
                if (remaining <= 0) break;

                var taken = Math.Min(batch.RemainingQuantity, remaining);
                var price = batch.Product?.SellingPrice ?? Product.SellingPrice;
                var discount = batch.DiscountPercentage;
                total += taken * price * (1 - discount / 100m);
                remaining -= taken;
            }

            if (remaining > 0)
            {
                total += remaining * Product.SellingPrice;
            }

            return total;
        }

        public IEnumerable<(StockBatch batch, decimal returned)> AddReturnedQuantity(decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (ReturnedQuantity + quantity > Quantity)
                throw new ReturnQuantityExceededException(Id, quantity, Quantity - ReturnedQuantity);

            ReturnedQuantity += quantity;

            var result = new List<(StockBatch batch, decimal returned)>();
            var remaining = quantity;

            foreach (var alloc in _allocations.Where(a => a.RemainingToReturn > 0))
            {
                if (remaining <= 0) break;
                
                var toReturn = Math.Min(alloc.RemainingToReturn, remaining);
                alloc.AddReturn(toReturn);
                result.Add((alloc.StockBatch, toReturn));
                
                remaining -= toReturn;
            }

            return result;
        }
    }
}
