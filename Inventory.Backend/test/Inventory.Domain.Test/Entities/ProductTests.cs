using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class ProductTests
    {

        [Fact]
        public void Product_Constructor_WithValidData_ShouldSetProperties()
        {
            var product = new Product("sku", "name", 10, 1);
            Assert.Equal("sku", product.SKU);
            Assert.Equal("name", product.Name);
            Assert.Equal(10, product.SellingPrice);
            Assert.Equal(1, product.ReorderPoint);
        }

        [Theory]
        [InlineData("", "name", 10, 1)]
        [InlineData(null, "name", 10, 1)]
        [InlineData("sku", "", 10, 1)]
        [InlineData("sku", null, 10, 1)]
        [InlineData("sku", "name", -1, 1)]
        [InlineData("sku", "name", 10, -1)]
        public void Product_Constructor_WithInvalidData_ShouldThrow(string? sku, string? name, decimal price, int reorder)
        {
            Assert.ThrowsAny<System.Exception>(() => new Product(sku!, name!, price, reorder));
        }

        [Fact]
        public void Rename_WhenValid_ShouldUpdateName()
        {
            var product = new Product("sku", "name", 10, 1);
            product.Rename("newName");
            Assert.Equal("newName", product.Name);
        }

        [Theory]
        [InlineData("")]
        public void Rename_WhenInvalid_ShouldThrow(string name)
        {
            var product = new Product("sku", "name", 10, 1);
            Assert.Throws<System.ArgumentException>(() => product.Rename(name));
        }
    }
}
