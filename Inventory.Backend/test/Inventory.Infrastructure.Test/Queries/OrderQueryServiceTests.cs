using FluentAssertions;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Queries;
using Inventory.Infrastructure.Test.Helpers;
using Moq;
using Xunit;

namespace Inventory.Infrastructure.Test.Queries;

public class OrderQueryServiceTests
{
    private readonly Mock<ILocalizationService> _localizationMock;

    public OrderQueryServiceTests()
    {
        _localizationMock = new Mock<ILocalizationService>();

        _localizationMock
            .Setup(x => x.GetMessage("OrderNotFound"))
            .Returns("Order not found");

        _localizationMock
            .Setup(x => x.GetMessage("DraftOrderAccessDenied"))
            .Returns("Draft order access denied");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Order_When_Order_Exists()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Supplier",
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

        product.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            20,
            0);
        var order = Order.CreateDraft(
            "cashier-1");

        order.AddItem(product, 2);

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetByIdAsync("cashier-1", order.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Id.Should().Be(order.Id);

        result.Value.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Failure_When_Order_Does_Not_Exist()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetByIdAsync("cashier-1", 999);

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
            "Supplier",
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

        product.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            20,
            0);

        await context.SaveChangesAsync();

        var draftOrder = Order.CreateDraft(
            "cashier-1");

        draftOrder.AddItem(product, 1);

        var completedOrder = Order.CreateDraft(
            "cashier-2"
            );

        completedOrder.AddItem(product, 1);

        completedOrder.Confirm(PaymentMethod.Cash, OrderType.InStore);

        context.Orders.AddRange(
            draftOrder,
            completedOrder);

        await context.SaveChangesAsync();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        var filter = new OrderFilter
        {
            Status = OrderStatus.Draft
        };

        // Act
        var result = await service.GetAllAsync(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Items.Should().ContainSingle();

        result.Value.Items.First().Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task GetAllAsync_Should_Apply_Pagination()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Supplier",
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

        product.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            50,
            0);
        for (int i = 0; i < 5; i++)
        {
            var order = Order.CreateDraft(
                $"cashier-{i}");

            order.AddItem(product, 1);

            context.Orders.Add(order);
        }

        await context.SaveChangesAsync();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        var filter = new OrderFilter
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
    public async Task GetItemsByOrderIdAsync_Should_Return_Order_Items()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Supplier",
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

        product.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            20,
            0);
        var order = Order.CreateDraft(
            "cashier-1");
        order.AddItem(product, 2);

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetItemsByOrderIdAsync("cashier-1", order.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().ContainSingle();

        result.Value.First().ProductName.Should().Be("Laptop");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Forbidden_When_Draft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var order = Order.CreateDraft("owner-cashier");
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetByIdAsync("other-cashier", order.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
        result.Error.Type.Should().Be(Inventory.Domain.Shared.ErrorType.Forbidden);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Order_When_NonDraft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier("Supplier", "01000000000");
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var product = new Product("P-001", "Laptop", 1000, 5);
        context.Products.Add(product);
        await context.SaveChangesAsync();

        product.AddStock(supplier.Id, DateTime.UtcNow.AddDays(30), 500, 20, 0);

        var order = Order.CreateDraft("owner-cashier");
        order.AddItem(product, 1);
        order.Confirm(PaymentMethod.Cash, OrderType.InStore);

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetByIdAsync("other-cashier", order.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id);
        result.Value.Status.Should().Be(OrderStatus.Completed);
    }

    [Fact]
    public async Task GetItemsByOrderIdAsync_Should_Return_Forbidden_When_Draft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var order = Order.CreateDraft("owner-cashier");
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new OrderQueryService(
            context,
            _localizationMock.Object);

        // Act
        var result = await service.GetItemsByOrderIdAsync("other-cashier", order.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
        result.Error.Type.Should().Be(Inventory.Domain.Shared.ErrorType.Forbidden);
    }
}
