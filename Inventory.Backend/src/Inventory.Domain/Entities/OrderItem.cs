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
        public decimal TotalPrice => Quantity * UnitPrice;

        private readonly List<OrderItemBatchAllocation> _allocations = new();
        public IReadOnlyCollection<OrderItemBatchAllocation> Allocations => _allocations.AsReadOnly();

        // Required by EF Core
        private OrderItem() { }

        
        internal OrderItem(int orderId, Product product, decimal quantity)
        {
            if (product is null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

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
                _allocations.Add(new OrderItemBatchAllocation(a.batch.Id, a.taken));
            }
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
