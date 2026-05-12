using FluentAssertions;
using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.Domain.Tests.Entities;

public class SupplierTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateSupplier()
    {
        // Arrange & Act
        var supplier = new Supplier(
            "Tech Supplier",
            "01012345678",
            "tech@gmail.com",
            "Cairo");

        // Assert
        supplier.Name.Should().Be("Tech Supplier");
        supplier.PhoneNumber.Should().Be("01012345678");
        supplier.ContactInfo.Should().Be("tech@gmail.com");
        supplier.Address.Should().Be("Cairo");
        supplier.TotalRating.Should().Be(0);
        supplier.RatingCount.Should().Be(0);
        supplier.AvgRating.Should().Be(0);
        supplier.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowException()
    {
        // Arrange
        Action act = () => new Supplier("", "01012345678");

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Name cannot be empty*");
    }

    [Fact]
    public void Constructor_WithEmptyPhoneNumber_ShouldThrowException()
    {
        // Arrange
        Action act = () => new Supplier("Supplier", "");

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Phone number cannot be empty*");
    }

    [Fact]
    public void Rename_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var supplier = new Supplier("Old Name", "01012345678");

        // Act
        supplier.Rename("New Name");

        // Assert
        supplier.Name.Should().Be("New Name");
    }

    [Fact]
    public void Rename_WithWhiteSpaceName_ShouldThrowException()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        Action act = () => supplier.Rename("   ");

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateContactInfo_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        supplier.UpdateContactInfo(
            "01111111111",
            "new@gmail.com",
            "Alex");

        // Assert
        supplier.PhoneNumber.Should().Be("01111111111");
        supplier.ContactInfo.Should().Be("new@gmail.com");
        supplier.Address.Should().Be("Alex");
    }

    [Fact]
    public void AddRating_WithValidRating_ShouldUpdateRatings()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        supplier.AddRating(4);

        // Assert
        supplier.TotalRating.Should().Be(4);
        supplier.RatingCount.Should().Be(1);
        supplier.AvgRating.Should().Be(4);
    }

    [Fact]
    public void AddRating_WithMultipleRatings_ShouldCalculateAverageCorrectly()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        supplier.AddRating(5);
        supplier.AddRating(3);

        // Assert
        supplier.TotalRating.Should().Be(8);
        supplier.RatingCount.Should().Be(2);
        supplier.AvgRating.Should().Be(4);
    }

    [Fact]
    public void AddRating_WithInvalidRating_ShouldThrowException()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        Action act = () => supplier.AddRating(10);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddRating_WithNote_ShouldAddSupplierNote()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        supplier.AddRating(5, "Excellent supplier");

        // Assert
        supplier.SupplierNotes.Should().HaveCount(1);

        supplier.SupplierNotes.First().Note
            .Should().Be("Excellent supplier");
    }

    [Fact]
    public void RegisterDelivery_WithValidTime_ShouldUpdateAverageDeliveryTime()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        supplier.RegisterDelivery(4);
        supplier.RegisterDelivery(6);

        // Assert
        supplier.DeliveryCount.Should().Be(2);
        supplier.AvgDeliveryTime.Should().Be(5);
    }

    [Fact]
    public void RegisterDelivery_WithNegativeTime_ShouldThrowException()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        // Act
        Action act = () => supplier.RegisterDelivery(-1);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkAsDeleted_ShouldSetIsDeletedToTrue()
    {
        // Arrange
        var supplier = new Supplier("Supplier","01012345678");

        // Act
        supplier.MarkAsDeleted();

        // Assert
        supplier.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Restore_ShouldSetIsDeletedToFalse()
    {
        // Arrange
        var supplier = new Supplier("Supplier", "01012345678");

        supplier.MarkAsDeleted();

        // Act
        supplier.Restore();

        // Assert
        supplier.IsDeleted.Should().BeFalse();
    }
}