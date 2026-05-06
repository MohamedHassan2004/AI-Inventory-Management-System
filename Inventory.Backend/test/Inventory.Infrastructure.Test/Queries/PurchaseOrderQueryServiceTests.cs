using FluentAssertions;
using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Queries;
using Inventory.Infrastructure.Test.Helpers;
using Moq;
using Xunit;

namespace Inventory.Infrastructure.Test.Queries;

public class PurchaseOrderQueryServiceTests
{
    private readonly Mock<ILocalizationService> _localizationMock;

    public PurchaseOrderQueryServiceTests()
    {
        _localizationMock = new Mock<ILocalizationService>();

        _localizationMock
            .Setup(x => x.GetMessage("PurchaseOrderNotFound"))
            .Returns("Purchase order not found");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_PurchaseOrder_When_Order_Exists()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Main Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product = new Product(
            "P-001",
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

        var service = new PurchaseOrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetByIdAsync(purchaseOrder.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Id.Should().Be(purchaseOrder.Id);

        result.Value.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Failure_When_Order_Does_Not_Exist()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var service = new PurchaseOrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        result.IsSuccess.Should().BeFalse();

        result.Error.Code.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetAllAsync_Should_Filter_By_Status()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Main Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product = new Product(
            "P-001",
            "Laptop",
            1000,
            5);

        context.Products.Add(product);

        await context.SaveChangesAsync();

        var pendingOrder = PurchaseOrder.Create(supplier.Id);

        pendingOrder.AddItem(
            product,
            5,
            500,
            DateTime.UtcNow.AddDays(30));

        var completedOrder = PurchaseOrder.Create(supplier.Id);

        completedOrder.AddItem(
            product,
            5,
            500,
            DateTime.UtcNow.AddDays(30));

        completedOrder.Complete();

        context.PurchaseOrders.AddRange(
            pendingOrder,
            completedOrder);

        await context.SaveChangesAsync();

        var service = new PurchaseOrderQueryService(
            context,
            _localizationMock.Object);

        var filter = new PurchaseOrderFilter
        {
            Status = PurchaseOrderStatus.Pending
        };

        // Act
        var result = await service.GetAllAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Items.Should().ContainSingle();

        result.Value.Items.First().Status.Should()
            .Be(PurchaseOrderStatus.Pending);
    }

    [Fact]
    public async Task GetAllAsync_Should_Apply_Pagination()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Main Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product = new Product(
            "P-001",
            "Laptop",
            1000,
            5);

        context.Products.Add(product);

        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            var purchaseOrder = PurchaseOrder.Create(supplier.Id);

            purchaseOrder.AddItem(
                product,
                5,
                500,
                DateTime.UtcNow.AddDays(30));

            context.PurchaseOrders.Add(purchaseOrder);
        }

        await context.SaveChangesAsync();

        var service = new PurchaseOrderQueryService(
            context,
            _localizationMock.Object);

        var filter = new PurchaseOrderFilter
        {
            Page = 1,
            PageSize = 2
        };

        // Act
        var result = await service.GetAllAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Items.Should().HaveCount(2);

        result.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetItemsByPurchaseOrderIdAsync_Should_Return_Order_Items()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Main Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product = new Product(
            "P-001",
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

        var service = new PurchaseOrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service
            .GetItemsByPurchaseOrderIdAsync(purchaseOrder.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().ContainSingle();

        result.Value.First().ProductName.Should().Be("Laptop");
    }
}