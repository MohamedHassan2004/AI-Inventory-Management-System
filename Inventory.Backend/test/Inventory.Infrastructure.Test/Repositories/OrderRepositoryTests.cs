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
            "cashier-1",
            OrderType.InStore);

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetDraftByIdAsync(order.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public async Task GetDraftByIdAsync_Should_Return_Null_When_Order_Is_Not_Draft()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var product = new Product(
        "SKU-1",
        "Laptop",
        1000,
        10);

        var order = Order.Submit(
            "cashier-1",
            new List<(Product product, decimal quantity)>
            {
                (product, 1)
            },
            PaymentMethod.Cash,
            OrderType.InStore,
            0);

        context.Orders.Add(order);

        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetDraftByIdAsync(order.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExpiredDraftsAsync_Should_Return_Expired_Drafts()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var expiredDraft = Order.CreateDraft(
            "cashier-1",
            OrderType.InStore);

        var validDraft = Order.CreateDraft(
            "cashier-2",
            OrderType.InStore);

        context.Orders.AddRange(expiredDraft, validDraft);

        await context.SaveChangesAsync();

        var repository = new OrderRepository(context);

        // Act
        var result = await repository.GetExpiredDraftsAsync(
            DateTime.UtcNow.AddHours(13));

        // Assert
        result.Should().ContainSingle();
    }
}