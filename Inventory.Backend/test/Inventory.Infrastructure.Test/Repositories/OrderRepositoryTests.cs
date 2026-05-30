using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class OrderRepositoryTests
{
    [Fact]
    public async Task GetDraftByIdAsync_Should_Return_Draft_Order()
    {

        // Arrange
        await using var context = DbContextFactory.Create();

        var order = Order.CreateDraft(
            "cashier-1");

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetDraftForMutationAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task GetDraftByIdAsync_Should_Return_Null_When_Order_Is_Not_Draft()
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

        product.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            10);

        await context.SaveChangesAsync();

        var order = Order.CreateDraft(
            "cashier-1");

        order.AddItem(product, 1);

        order.Confirm(PaymentMethod.Cash, OrderType.InStore);

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetDraftForMutationAsync(order.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpiredDraftsAsync_Should_Return_Expired_Drafts()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var expiredOrder = Order.CreateDraft(
            "cashier-1");
        var validOrder = Order.CreateDraft(
            "cashier-2");

        typeof(Order)
            .GetProperty(nameof(Order.ExpiresAt))!
            .SetValue(expiredOrder, DateTime.UtcNow.AddHours(-1));

        typeof(Order)
            .GetProperty(nameof(Order.ExpiresAt))!
            .SetValue(validOrder, DateTime.UtcNow.AddDays(2));

        context.Orders.AddRange(
            expiredOrder,
            validOrder);

        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetExpiredDraftsForCleanupAsync(
            DateTime.UtcNow);

        // Assert
        result.Should().ContainSingle();

        result.First().CashierId.Should().Be("cashier-1");
    }
}