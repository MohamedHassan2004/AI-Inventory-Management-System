using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class StockBatchTests
    {

        [Fact]
        public void StockBatch_Constructor_WithValidData_ShouldSetProperties()
        {
            var expire = System.DateTime.UtcNow.AddDays(10);
            var batch = new StockBatch(1, 2, expire, 10, 5);
            Assert.Equal(1, batch.ProductId);
            Assert.Equal(2, batch.SupplierId);
            Assert.Equal(10, batch.UnitCost);
            Assert.Equal(5, batch.OriginalQuantity);
            Assert.Equal(5, batch.RemainingQuantity);
            Assert.Equal(expire, batch.ExpireDate);
        }

        [Theory]
        [InlineData(0, 1, 10, 5)]
        [InlineData(1, 0, 10, 5)]
        [InlineData(1, 1, 10, 0)]
        [InlineData(1, 1, 10, -1)]
        public void StockBatch_Constructor_WithInvalidIdsOrQuantity_ShouldThrow(int productId, int supplierId, decimal unitCost, decimal quantity)
        {
            var expire = System.DateTime.UtcNow.AddDays(10);
            Assert.ThrowsAny<System.Exception>(() => new StockBatch(productId, supplierId, expire, unitCost, quantity));
        }

        [Fact]
        public void StockBatch_Constructor_WithPastExpireDate_ShouldThrow()
        {
            Assert.Throws<System.ArgumentException>(() => new StockBatch(1, 2, System.DateTime.UtcNow.AddDays(-1), 10, 5));
        }

        [Fact]
        public void StockBatch_Constructor_WithNegativeUnitCost_ShouldThrow()
        {
            var expire = System.DateTime.UtcNow.AddDays(10);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new StockBatch(1, 2, expire, -1, 5));
        }
    }
}
