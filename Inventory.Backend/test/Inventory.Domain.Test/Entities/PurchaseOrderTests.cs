using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class PurchaseOrderTests
    {

        [Fact]
        public void PurchaseOrder_Create_WithValidSupplierId_ShouldSetProperties()
        {
            var po = PurchaseOrder.Create(1);
            Assert.Equal(1, po.SupplierId);
            Assert.Equal(Inventory.Domain.Enums.PurchaseOrderStatus.Pending, po.Status);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PurchaseOrder_Create_WithInvalidSupplierId_ShouldThrow(int supplierId)
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => PurchaseOrder.Create(supplierId));
        }
    }
}
