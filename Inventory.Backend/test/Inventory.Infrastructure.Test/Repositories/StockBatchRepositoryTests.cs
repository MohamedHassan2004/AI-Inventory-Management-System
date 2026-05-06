using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class StockBatchRepositoryTests
{
    [Fact]
    public async Task GetByProductIdAsync_Should_Return_Product_Batches_Only()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Main Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product1 = new Product(
            "P-001",
            "Laptop",
            1000,
            5);

        var product2 = new Product(
            "P-002",
            "Mouse",
            100,
            2);

        context.Products.AddRange(product1, product2);

        await context.SaveChangesAsync();

        product1.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            10);

        product2.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            50,
            20);

        await context.SaveChangesAsync();

        var repository = new StockBatchRepository(context);

        // Act
        var result = await repository.GetByProductIdAsync(product1.Id);

        // Assert
        result.Should().ContainSingle();

        result.First().ProductId.Should().Be(product1.Id);
    }

    [Fact]
    public async Task GetWithDetailsAsync_Should_Return_Batch_With_Product_And_Supplier()
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

        var batch = context.StockBatches.First();

        var repository = new StockBatchRepository(context);

        // Act
        var result = await repository.GetWithDetailsAsync(batch.Id);

        // Assert
        result.Should().NotBeNull();

        result!.Product.Should().NotBeNull();

        result.Supplier.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllWithDetailsAsync_Should_Return_All_Batches_With_Details()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Main Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product1 = new Product(
            "P-001",
            "Laptop",
            1000,
            5);

        var product2 = new Product(
            "P-002",
            "Mouse",
            100,
            2);

        context.Products.AddRange(product1, product2);

        await context.SaveChangesAsync();

        product1.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            10);

        product2.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(60),
            50,
            20);

        await context.SaveChangesAsync();

        var repository = new StockBatchRepository(context);

        // Act
        var result = await repository.GetAllWithDetailsAsync();

        // Assert
        result.Should().HaveCount(2);

        result.Should().OnlyContain(b =>
            b.Product != null &&
            b.Supplier != null);
    }

    [Fact]
    public async Task GetExpiringBatchesAsync_Should_Return_Expiring_Batches_Ordered_By_ExpireDate()
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
            DateTime.UtcNow.AddDays(5),
            500,
            10);

        product.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(20),
            500,
            10);

        await context.SaveChangesAsync();

        var repository = new StockBatchRepository(context);

        // Act
        var result = (await repository.GetExpiringBatchesAsync(
            DateTime.UtcNow.AddDays(10)))
            .ToList();

        // Assert
        result.Should().ContainSingle();

        result.First().ExpireDate.Should().BeOnOrBefore(
            DateTime.UtcNow.AddDays(10));
    }
}