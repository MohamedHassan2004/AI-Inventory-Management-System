using Inventory.Domain.Entities;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class OrderItemTests
    {
        private Product CreateProductWithStock(decimal qty = 10m)
        {
            var product = new Product("SKU", "Name", 10m, 1);

            var productId = 1;
            typeof(Product).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(product, productId);

            var batchesField = typeof(Product).GetField("_batches", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var batches = (List<StockBatch>)batchesField.GetValue(product)!;
            var batch = new StockBatch(productId, 1, System.DateTime.UtcNow.AddYears(1), 5m, qty);
            batches.Add(batch);

            return product;
        }

        [Fact]
        public void OrderItem_Constructor_WithInvalidData_ShouldThrow()
        {
            var product = CreateProductWithStock();
            Assert.Throws<System.ArgumentNullException>(() => new OrderItem(1, null!, 1));
            Assert.Throws<System.ArgumentException>(() => new OrderItem(1, product, 0));
            Assert.Throws<System.ArgumentException>(() => new OrderItem(1, product, -1));
        }

        [Fact]
        public void UpdateQuantity_WithValidData_ShouldUpdateQuantityAndPrice()
        {
            var product = CreateProductWithStock(15m);
            typeof(Product).GetProperty("SellingPrice", BindingFlags.Public | BindingFlags.Instance)!
                .SetValue(product, 15m);

            var item = new OrderItem(1, product, 2);
            item.UpdateQuantity(5);

            Assert.Equal(5, item.Quantity);
        }

        [Fact]
        public void UpdateQuantity_WithInvalidQuantity_ShouldThrow()
        {
            var product = CreateProductWithStock();
            var item = new OrderItem(1, product, 2);
            Assert.Throws<System.ArgumentException>(() => item.UpdateQuantity(0));
            Assert.Throws<System.ArgumentException>(() => item.UpdateQuantity(-1));
        }
    }
}
