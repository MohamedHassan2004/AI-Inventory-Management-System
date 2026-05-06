using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class SupplierRepositoryTests
{
    [Fact]
    public async Task ExistsAsync_Should_Return_True_When_Supplier_Exists()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Suppliers.Add(
            new Supplier(
                "Tech Supplier",
                "01000000000"));

        await context.SaveChangesAsync();

        var repository = new SupplierRepository(context);

        // Act
        var result = await repository.ExistsAsync("Tech Supplier");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_Should_Return_False_When_Supplier_Does_Not_Exist()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var repository = new SupplierRepository(context);

        // Act
        var result = await repository.ExistsAsync("Unknown Supplier");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetSupplierWithNotesAsync_Should_Return_Supplier_With_Notes()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Tech Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        context.SupplierNotes.Add(
            new SupplierNotes
            {
                SupplierId = supplier.Id,
                Note = "Preferred supplier"
            });

        await context.SaveChangesAsync();

        var repository = new SupplierRepository(context);

        // Act
        var result = await repository.GetSupplierWithNotesAsync(supplier.Id);

        // Assert
        result.Should().NotBeNull();

        result!.SupplierNotes.Should().NotBeEmpty();

        result.SupplierNotes.First().Note.Should().Be("Preferred supplier");
    }

    [Fact]
    public async Task GetSupplierWithNotesAsync_Should_Return_Null_When_Supplier_Does_Not_Exist()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var repository = new SupplierRepository(context);

        // Act
        var result = await repository.GetSupplierWithNotesAsync(999);

        // Assert
        result.Should().BeNull();
    }
}