using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class PurchaseOrderItemTests
    {

        [Fact]
        public void PurchaseOrderItem_Constructor_WithNullProduct_ShouldThrow()
        {
            Assert.Throws<System.ArgumentNullException>(() => new PurchaseOrderItem(null!, 1, 1, System.DateTime.UtcNow.AddDays(1)));
        }

        [Fact]
        public void PurchaseOrderItem_Constructor_WithInvalidQuantity_ShouldThrow()
        {
            var product = new Product("SKU", "Name", 10m, 1);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PurchaseOrderItem(product, 0, 1, System.DateTime.UtcNow.AddDays(1)));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PurchaseOrderItem(product, -1, 1, System.DateTime.UtcNow.AddDays(1)));
        }

        [Fact]
        public void PurchaseOrderItem_Constructor_WithInvalidCost_ShouldThrow()
        {
            var product = new Product("SKU", "Name", 10m, 1);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PurchaseOrderItem(product, 1, -1, System.DateTime.UtcNow.AddDays(1)));
        }

        [Fact]
        public void TotalPrice_ShouldCalculateCorrectly()
        {
            var product = new Product("SKU", "Name", 15m, 1);
            var expiry = System.DateTime.UtcNow.AddDays(10);
            var item = new PurchaseOrderItem(product, 2, 5, expiry);
            Assert.Equal(10, item.TotalPrice);
        }

        [Fact]
        public void PurchaseOrderItem_Constructor_WithValidData_ShouldSetProperties()
        {
            var product = new Product("sku", "name", 10, 1);
            var expiry = System.DateTime.UtcNow.AddDays(1);
            var item = new PurchaseOrderItem(product, 2, 5, expiry);
            Assert.Equal(product, item.Product);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(5, item.UnitCost);
            Assert.Equal(expiry, item.ExpiryDate);
        }
    }
}
