using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Test.Entities
{
    public class SupplierTests
    {
        [Fact]
        public void CanCreateSupplier()
        {
            var supplier = new Supplier();
            Assert.NotNull(supplier);
        }

        [Fact]
        public void Supplier_Constructor_WithValidData_ShouldSetProperties()
        {
            var supplier = new Supplier("SupplierName", "0123456789", "contact", "address");
            Assert.Equal("SupplierName", supplier.Name);
            Assert.Equal("0123456789", supplier.PhoneNumber);
            Assert.Equal("contact", supplier.ContactInfo);
            Assert.Equal("address", supplier.Address);
        }

        [Theory]
        [InlineData(null, "0123456789")]
        [InlineData("", "0123456789")]
        [InlineData("SupplierName", null)]
        [InlineData("SupplierName", "")]
        public void Supplier_Constructor_WithInvalidData_ShouldThrow(string name, string phone)
        {
            Assert.Throws<System.ArgumentException>(() => new Supplier(name, phone));
        }

        [Fact]
        public void AddRating_WithValidRating_ShouldUpdateTotals()
        {
            var supplier = new Supplier("SupplierName", "0123456789");
            supplier.AddRating(5, null);
            Assert.Equal(5, supplier.TotalRating);
            Assert.Equal(1, supplier.RatingCount);
            Assert.Equal(5, supplier.AvgRating);
        }

        [Fact]
        public void AddRating_WithInvalidRating_ShouldThrow()
        {
            var supplier = new Supplier("SupplierName", "0123456789");
            Assert.Throws<System.ArgumentOutOfRangeException>(() => supplier.AddRating(-1, null));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => supplier.AddRating(6, null));
        }

        [Fact]
        public void AddRating_WithNote_ShouldAddSupplierNote()
        {
            var supplier = new Supplier("SupplierName", "0123456789");
            supplier.AddRating(4, "Great service");
            Assert.Single(supplier.SupplierNotes);
            Assert.Equal("Great service", supplier.SupplierNotes[0].Note);
        }

        [Fact]
        public void UpdatePhoneNumber_WithValid_ShouldUpdate()
        {
            var supplier = new Supplier("SupplierName", "0123456789");
            supplier.UpdatePhoneNumber("0987654321");
            Assert.Equal("0987654321", supplier.PhoneNumber);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void UpdatePhoneNumber_WithInvalid_ShouldThrow(string phone)
        {
            var supplier = new Supplier("SupplierName", "0123456789");
            Assert.Throws<System.ArgumentException>(() => supplier.UpdatePhoneNumber(phone));
        }

        [Fact]
        public void MarkAsDeleted_WhenCalled_ShouldSetIsDeleted()
        {
            var supplier = new Supplier("SupplierName", "0123456789");
            supplier.MarkAsDeleted();
            Assert.True(supplier.IsDeleted);
        }
    }
}
