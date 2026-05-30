using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using System.Reflection;
using Xunit;
using System.Linq;

namespace Inventory.Domain.Test.Entities
{
    public class OrderTests
    {
        private const decimal DefaultTaxPercentage = 0m;

        // ─── Helpers ────────────────────────────────────────────────────────────

        private void SetId(object entity, int id)
        {
            var property = entity.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            property?.SetValue(entity, id);
        }

        // Creates a product with one stock batch that has the specified quantity.
        private (Product product, StockBatch batch) CreateProductWithStock(decimal stockQty, decimal sellingPrice = 100m)
        {
            var product = new Product("SKU1", "Product 1", sellingPrice, 1);
            SetId(product, 10);

            var batchesField = typeof(Product).GetField("_batches", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var batches = (List<StockBatch>)batchesField.GetValue(product)!;

            var batch = new StockBatch(product.Id, 1, DateTime.UtcNow.AddYears(1), 50m, stockQty);
            SetId(batch, 1);

            // Back-reference needed so allocation.StockBatch.Restore() works in tests
            typeof(StockBatch).GetProperty("Product", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(batch, product);

            batches.Add(batch);
            return (product, batch);
        }

        private Order ConfirmDraft(Product product, decimal quantity, OrderType orderType)
        {
            var order = Order.CreateDraft("cashier1");
            order.AddItem(product, quantity);
            order.Confirm(PaymentMethod.Cash, orderType);
            return order;
        }

        // ─── Existing Draft Mutation Tests ──────────────────────────────────────

        [Fact]
        public void AddItem_WhenItemAlreadyExists_ShouldAggregateQuantity()
        {
            var (product, _) = CreateProductWithStock(20);

            var order = Order.CreateDraft("cashier1");
            order.AddItem(product, 2);
            order.AddItem(product, 4);

            Assert.Single(order.Items);
            Assert.Equal(6, order.Items.First().Quantity);
        }

        [Fact]
        public void AddItem_ShouldRecalculateFinancialTotals()
        {
            var (product, _) = CreateProductWithStock(10, sellingPrice: 150m);
            var order = Order.CreateDraft("cashier1");

        order.AddItem(product, 2); // 2 * 150 = 300 SubTotal

            Assert.Equal(300m, order.SubTotal);

            var expectedTax = 300m * DefaultTaxPercentage / 100m;
            Assert.Equal(expectedTax, order.TaxAmount);
            Assert.Equal(300m + expectedTax, order.FinalTotal);
        }

        [Fact]
        public void Confirm_ShouldRecalculateFinancialTotals()
        {
            var (product, _) = CreateProductWithStock(10, sellingPrice: 200m);
            var order = ConfirmDraft(product, 2, OrderType.InStore);

            Assert.Equal(400m, order.SubTotal);
            var expectedTax = 400m * DefaultTaxPercentage / 100m;
            Assert.Equal(expectedTax, order.TaxAmount);
            Assert.Equal(400m + expectedTax, order.FinalTotal);
        }

        [Fact]
        public void UpdateItemQuantity_WithValidQuantity_ShouldUpdateQuantityAndRecalculate()
        {
            var (product, _) = CreateProductWithStock(20);
            var order = Order.CreateDraft("cashier1");
            order.AddItem(product, 2);

            order.UpdateItemQuantity(10, 5);

            Assert.Single(order.Items);
            Assert.Equal(5, order.Items.First().Quantity);
            Assert.Equal(500m, order.SubTotal); // 5 * 100
        }

        [Fact]
        public void UpdateItemQuantity_WithInvalidQuantity_ShouldThrow()
        {
            var (product, _) = CreateProductWithStock(10);
            var order = Order.CreateDraft("cashier1");
            order.AddItem(product, 2);

            Assert.Throws<ArgumentException>(() => order.UpdateItemQuantity(10, 0));
            Assert.Throws<ArgumentException>(() => order.UpdateItemQuantity(10, -1));
        }

        [Fact]
        public void UpdateItemQuantity_ForNonExistentProduct_ShouldThrow()
        {
            var (product, _) = CreateProductWithStock(10);
            var order = Order.CreateDraft("cashier1");
            order.AddItem(product, 2);

            Assert.Throws<InvalidOperationException>(() => order.UpdateItemQuantity(99, 5));
        }

        // ─── Confirm Tests ──────────────────────────────────────────────────────

        [Fact]
        public void Confirm_InStoreOrder_ShouldAllocateStockAndSetCompleted()
        {
            var (product, batch) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 3, OrderType.InStore);

            Assert.Equal(OrderStatus.Completed, order.Status);
            Assert.True(order.Items.First().Allocations.Any());
            Assert.Equal(7, batch.RemainingQuantity); // 10 - 3
        }

        [Fact]
        public void Confirm_DeliveryOrder_ShouldAllocateStockAndSetOutForDelivery()
        {
            var (product, batch) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 4, OrderType.Delivery);

            Assert.Equal(OrderStatus.OutForDelivery, order.Status);
            Assert.True(order.Items.First().Allocations.Any());
            Assert.Equal(6, batch.RemainingQuantity); // 10 - 4
        }

        [Fact]
        public void Confirm_EmptyOrder_ShouldThrow()
        {
            var order = Order.CreateDraft("cashier1");

            Assert.Throws<EmptyOrderException>(() => order.Confirm(PaymentMethod.Cash, OrderType.InStore));
        }

        [Fact]
        public void Confirm_AlreadyCompleted_ShouldThrow()
        {
            var (product, _) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 3, OrderType.InStore);

            // Can't confirm again once it's Completed
            Assert.Throws<InvalidOperationException>(() => order.Confirm(PaymentMethod.Cash, OrderType.InStore));
        }

        // ─── MarkAsDelivered Tests ──────────────────────────────────────────────

        [Fact]
        public void MarkAsDelivered_WhenOutForDelivery_ShouldSetCompleted()
        {
            var (product, _) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 4, OrderType.Delivery);

            order.MarkAsDelivered();

            Assert.Equal(OrderStatus.Completed, order.Status);
        }

        [Fact]
        public void MarkAsDelivered_WhenNotOutForDelivery_ShouldThrow()
        {
            var (product, _) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 3, OrderType.InStore); // already Completed

            Assert.Throws<InvalidOperationException>(() => order.MarkAsDelivered());
        }

        [Fact]
        public void MarkAsDelivered_WhenDraft_ShouldThrow()
        {
            var order = Order.CreateDraft("cashier1");

            Assert.Throws<InvalidOperationException>(() => order.MarkAsDelivered());
        }

        // ─── FailDelivery Tests ──────────────────────────────────────────────────

        [Fact]
        public void FailDelivery_WhenOutForDelivery_ShouldCancelAndRestoreStock()
        {
            var (product, batch) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 4, OrderType.Delivery);
            Assert.Equal(6, batch.RemainingQuantity); // 10 - 4

            order.FailDelivery();

            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.Equal(10, batch.RemainingQuantity); // fully restored
        }

        [Fact]
        public void FailDelivery_WhenNotOutForDelivery_ShouldThrow()
        {
            var (product, _) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 3, OrderType.InStore); // Completed

            Assert.Throws<InvalidOperationException>(() => order.FailDelivery());
        }

        [Fact]
        public void FailDelivery_WhenDraft_ShouldThrow()
        {
            var order = Order.CreateDraft("cashier1");

            Assert.Throws<InvalidOperationException>(() => order.FailDelivery());
        }

        [Fact]
        public void FailDelivery_WithPartialReturnAlreadyProcessed_ShouldRestoreOnlyRemainingQuantity()
        {
            // Allocate 10 units → 0 remaining in batch
            var (product, batch) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 10, OrderType.Delivery);
            Assert.Equal(0, batch.RemainingQuantity);

            var allocation = order.Items.First().Allocations.First();

            // Simulate a partial return of 4 already processed via the internal AddReturn method
            var addReturn = typeof(OrderItemBatchAllocation)
                .GetMethod("AddReturn", BindingFlags.NonPublic | BindingFlags.Instance)!;
            addReturn.Invoke(allocation, new object[] { 4m });

            // The return also restored the batch
            batch.Restore(4m);
            Assert.Equal(4, batch.RemainingQuantity);
            Assert.Equal(6, allocation.RemainingToReturn); // 10 - 4

            // Act: fail the delivery → should restore only the remaining 6
            order.FailDelivery();

            Assert.Equal(10, batch.RemainingQuantity); // 4 already restored + 6 by FailDelivery
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        [Fact]
        public void FailDelivery_WhenAllQuantityAlreadyReturned_ShouldNotRestoreAnything()
        {
            // Allocate 5, mark all 5 as returned → RemainingToReturn == 0
            var (product, batch) = CreateProductWithStock(5);
            var order = ConfirmDraft(product, 5, OrderType.Delivery);
            Assert.Equal(0, batch.RemainingQuantity);

            var allocation = order.Items.First().Allocations.First();

            var addReturn = typeof(OrderItemBatchAllocation)
                .GetMethod("AddReturn", BindingFlags.NonPublic | BindingFlags.Instance)!;
            addReturn.Invoke(allocation, new object[] { 5m });
            batch.Restore(5m);

            Assert.Equal(5, batch.RemainingQuantity);
            Assert.Equal(0, allocation.RemainingToReturn);

            order.FailDelivery();

            // Nothing extra restored — batch stays at 5
            Assert.Equal(5, batch.RemainingQuantity);
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        // ─── Cancel Tests ────────────────────────────────────────────────────────

        [Fact]
        public void Cancel_WhenDraft_ShouldSetCancelled()
        {
            var order = Order.CreateDraft("cashier1");
            order.Cancel();
            Assert.Equal(OrderStatus.Cancelled, order.Status);
        }

        [Fact]
        public void Cancel_WhenOutForDelivery_ShouldThrow_MustUseFailDelivery()
        {
            var (product, _) = CreateProductWithStock(10);
            var order = ConfirmDraft(product, 4, OrderType.Delivery);

            // Must use FailDelivery() — Cancel() is only for drafts
            Assert.Throws<InvalidOperationException>(() => order.Cancel());
        }
    }
}
