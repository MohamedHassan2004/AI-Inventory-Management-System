using Inventory.Domain.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Domain.Entities
{
    public record ReturnStockInfo(int SupplierId, decimal UnitCost, DateTime OriginalExpiryDate, decimal Quantity);

    public class OrderItem
    {
        public int Id { get; private set; }

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public byte[] RowVersion { get; private set; } = null!;

        public decimal UnitPrice { get; private set; }

        public decimal Quantity { get; private set; }
        public decimal ReturnedQuantity { get; private set; }
        public decimal TotalPrice => Quantity * UnitPrice;


        private readonly List<StockConsumption> _stockConsumptions = new();
        public IReadOnlyCollection<StockConsumption> StockConsumptions => _stockConsumptions.AsReadOnly();

        private OrderItem() { }

        public OrderItem(Product product, decimal quantity)
        {
            Product = product ?? throw new ArgumentNullException(nameof(product));
            ProductId = product.Id;
            UnitPrice = product.SellingPrice;
            if (product.StockQuantity < quantity)
                throw new InsufficientStockException(product.Name, quantity, product.StockQuantity);
            Quantity = quantity;
        }

        public void AddReturnedQuantity(decimal quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (ReturnedQuantity + quantity > Quantity)
                throw new ReturnQuantityExceededException(Id, quantity, Quantity - ReturnedQuantity);

            ReturnedQuantity += quantity;
        }

        public void AddStockConsumption(StockConsumption consumption)
        {
            if (consumption == null)
                throw new ArgumentNullException(nameof(consumption));

            var totalConsumed = _stockConsumptions.Sum(c => c.Quantity);
            if (totalConsumed + consumption.Quantity > Quantity)
            {
                throw new InvalidOperationException(
                    $"Cannot add consumption of {consumption.Quantity}. Total would exceed OrderItem quantity of {Quantity}.");
            }

            consumption.SetOrderItem(this);
            _stockConsumptions.Add(consumption);
        }

        public void ValidateConsumptions()
        {
            var totalConsumed = _stockConsumptions.Sum(c => c.Quantity);
            if (totalConsumed != Quantity)
            {
                throw new InvalidOperationException(
                    $"Stock consumption mismatch for item {Id}. Quantity is {Quantity} but total consumed is {totalConsumed}.");
            }
        }

        public void ValidateReturnedQuantityConsistency()
        {
            var totalReturnedInConsumptions = _stockConsumptions.Sum(c => c.ReturnedQuantity);
            
            // Rounding to 4 decimal places to handle precision issues during aggregation
            if (Math.Round(totalReturnedInConsumptions, 4) != Math.Round(ReturnedQuantity, 4))
            {
                throw new InvalidOperationException(
                    $"Consistency error: OrderItem {Id} has {ReturnedQuantity} returned, but StockConsumptions sum to {totalReturnedInConsumptions}.");
            }
        }

        /// <summary>
        /// Processes a return for this item by updating its status and distributing 
        /// the quantity across original stock consumptions (FIFO).
        /// </summary>
        public IEnumerable<ReturnStockInfo> Return(decimal quantity)
        {
            // 1. Update total returned quantity (validates against item quantity)
            AddReturnedQuantity(quantity);

            var stockToRestore = new List<ReturnStockInfo>();
            var remainingToReturn = quantity;

            // 2. Distribute across consumptions in stable FIFO order
            var sortedConsumptions = _stockConsumptions.OrderBy(c => c.Id).ToList();

            foreach (var consumption in sortedConsumptions)
            {
                if (remainingToReturn <= 0) break;

                if (consumption.RemainingToReturn > 0)
                {
                    var taking = Math.Min(remainingToReturn, consumption.RemainingToReturn);
                    
                    consumption.Return(taking);
                    
                    stockToRestore.Add(new ReturnStockInfo(
                        consumption.StockBatch.SupplierId, 
                        consumption.StockBatch.UnitCost, 
                        consumption.StockBatch.ExpireDate,
                        taking));

                    remainingToReturn -= taking;
                }
            }

            if (remainingToReturn > 0)
            {
                throw new InvalidOperationException(
                    $"Insufficient consumption history to fulfill the return of {quantity} units for product {Product.Name}.");
            }

            // 3. Final safety check
            ValidateReturnedQuantityConsistency();

            return stockToRestore;
        }
    }
}
