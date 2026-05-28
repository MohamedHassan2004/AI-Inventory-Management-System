using Inventory.Domain.Entities;
using System.Reflection;
using Xunit;
using System.Linq;

namespace Inventory.Domain.Test.Entities
{
    public class OrderTests
    {
        private void SetId(object entity, int id)
        {
            var property = entity.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            property?.SetValue(entity, id);
        }

        [Fact]
        public void AddItem_WhenItemAlreadyExists_ShouldAggregateQuantity()
        {
            // Arrange
            var order = Order.CreateDraft("cashier1");
            var product = new Product("SKU1", "Product 1", 100m, 1);
            SetId(product, 10);

            // Act
            order.AddItem(product, 2);
            order.AddItem(product, 4);

            // Assert
            Assert.Single(order.Items);
            var item = order.Items.First();
            Assert.Equal(6, item.Quantity);
        }

        [Fact]
        public void UpdateItemQuantity_WithValidQuantity_ShouldUpdateQuantityAndRecalculate()
        {
            // Arrange
            var order = Order.CreateDraft("cashier1");
            var product = new Product("SKU1", "Product 1", 100m, 1);
            SetId(product, 10);
            order.AddItem(product, 2);

            // Act
            order.UpdateItemQuantity(10, 5);

            // Assert
            Assert.Single(order.Items);
            var item = order.Items.First();
            Assert.Equal(5, item.Quantity);
            Assert.Equal(500m, order.SubTotal); // 5 * 100 = 500
        }

        [Fact]
        public void UpdateItemQuantity_WithInvalidQuantity_ShouldThrow()
        {
            // Arrange
            var order = Order.CreateDraft("cashier1");
            var product = new Product("SKU1", "Product 1", 100m, 1);
            SetId(product, 10);
            order.AddItem(product, 2);

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() => order.UpdateItemQuantity(10, 0));
            Assert.Throws<System.ArgumentException>(() => order.UpdateItemQuantity(10, -1));
        }

        [Fact]
        public void UpdateItemQuantity_ForNonExistentProduct_ShouldThrow()
        {
            // Arrange
            var order = Order.CreateDraft("cashier1");
            var product = new Product("SKU1", "Product 1", 100m, 1);
            SetId(product, 10);
            order.AddItem(product, 2);

            // Act & Assert
            Assert.Throws<System.InvalidOperationException>(() => order.UpdateItemQuantity(99, 5));
        }
    }
}
