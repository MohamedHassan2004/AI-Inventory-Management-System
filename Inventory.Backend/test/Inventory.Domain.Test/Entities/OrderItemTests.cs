using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class OrderItemTests
    {

        [Fact]
        public void OrderItem_Constructor_WithInvalidData_ShouldThrow()
        {
            var product = new Product("SKU", "Name", 10m, 1);
            Assert.Throws<System.ArgumentNullException>(() => new OrderItem(1, null!, 1));
            Assert.Throws<System.ArgumentException>(() => new OrderItem(1, product, 0));
            Assert.Throws<System.ArgumentException>(() => new OrderItem(1, product, -1));
        }

        [Fact]
        public void UpdateQuantity_WithValidData_ShouldUpdateQuantityAndPrice()
        {
            var product = new Product("SKU", "Name", 15m, 1);
            var item = new OrderItem(1, product, 2);
            item.UpdateQuantity(5, 20);

            Assert.Equal(5, item.Quantity);
            Assert.Equal(20, item.UnitPrice);
        }

        [Fact]
        public void UpdateQuantity_WithInvalidQuantity_ShouldThrow()
        {
            var product = new Product("SKU", "Name", 10m, 1);
            var item = new OrderItem(1, product, 2);
            Assert.Throws<System.ArgumentException>(() => item.UpdateQuantity(0, 10));
            Assert.Throws<System.ArgumentException>(() => item.UpdateQuantity(-1, 10));
        }
    }
}
