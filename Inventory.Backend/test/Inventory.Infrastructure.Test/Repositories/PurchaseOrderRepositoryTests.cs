using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class PurchaseOrderRepositoryTests
{
    [Fact]
    public async Task GetPurchaseOrderWithItemsAsync_Should_Return_PurchaseOrder_With_Items()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Tech Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product = new Product(
            "LAP-001",
            "Laptop",
            1000,
            5);

        context.Products.Add(product);

        await context.SaveChangesAsync();

        var purchaseOrder = PurchaseOrder.Create(supplier.Id);

        purchaseOrder.AddItem(
            product,
            5,
            500,
            DateTime.UtcNow.AddDays(30));

        context.PurchaseOrders.Add(purchaseOrder);

        await context.SaveChangesAsync();

        var repository = new PurchaseOrderRepository(context);

        // Act
        var result = await repository.GetPurchaseOrderWithItemsAsync(purchaseOrder.Id);

        // Assert
        result.Should().NotBeNull();

        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPurchaseOrderWithItemsAsync_Should_Return_Null_When_PurchaseOrder_Does_Not_Exist()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var repository = new PurchaseOrderRepository(context);

        // Act
        var result = await repository.GetPurchaseOrderWithItemsAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFullPurchaseOrderAsync_Should_Return_PurchaseOrder_With_Product_Details()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Tech Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product = new Product(
            "LAP-001",
            "Laptop",
            1000,
            5);

        context.Products.Add(product);

        await context.SaveChangesAsync();

        var purchaseOrder = PurchaseOrder.Create(supplier.Id);

        purchaseOrder.AddItem(
            product,
            5,
            500,
            DateTime.UtcNow.AddDays(30));

        context.PurchaseOrders.Add(purchaseOrder);

        await context.SaveChangesAsync();

        var repository = new PurchaseOrderRepository(context);

        // Act
        var result = await repository.GetFullPurchaseOrderAsync(purchaseOrder.Id);

        // Assert
        result.Should().NotBeNull();

        result!.Items.Should().NotBeEmpty();

        result.Items.First().Product.Should().NotBeNull();

        result.Items.First().Product.Name.Should().Be("Laptop");
    }
}