using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class ProductRepositoryTests
{
    [Fact]
    public async Task ExistsByNameAsync_Should_Return_True_When_Name_Exists()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Products.Add(
            new Product("SKU-1", "Laptop", 1000, 5));

        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.ExistsByNameAsync("Laptop");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsBySkuAsync_Should_Return_True_When_Sku_Exists()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Products.Add(
            new Product("LAP-001", "Laptop", 1000, 5));

        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.ExistsBySkuAsync("LAP-001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Empty_When_SearchTerm_Is_Too_Short()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.SearchAsync("A");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Product_When_Exact_Sku_Matches()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var product = new Product("LAP-001", "Laptop", 1000, 5);

        context.Products.Add(product);

        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.SearchAsync("LAP-001");

        // Assert
        result.Should().ContainSingle();
        result.First().SKU.Should().Be("LAP-001");
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Product_When_Name_Partially_Matches()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Products.Add(
            new Product("LAP-001", "Gaming Laptop", 1000, 5));

        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.SearchAsync("Laptop");

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Should_Return_Product_When_Fuzzy_Search_Matches()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Products.Add(
            new Product("LAP-001", "Laptop", 1000, 5));

        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.SearchAsync("Laptpo");

        // Assert
        result.Should().NotBeEmpty();
    }
    [Fact]
    public async Task GetWithBatchesAsync_Should_Return_Product_With_Batches()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var category = new Category("Electronics", "image.jpg");

        var supplier = new Supplier(
            "Tech Supplier",
            "01000000000");

        context.Categories.Add(category);
        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var product = new Product(
            "LAP-001",
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

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.GetWithBatchesAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Batches.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLowStockProductsAsync_Should_Return_Low_Stock_Products()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Tech Supplier",
            "01000000000");

        context.Suppliers.Add(supplier);

        await context.SaveChangesAsync();

        var lowStockProduct = new Product(
            "LOW-001",
            "Low Stock Laptop",
            1000,
            10);

        context.Products.Add(lowStockProduct);

        await context.SaveChangesAsync();

        lowStockProduct.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            5);

        var normalStockProduct = new Product(
            "NOR-001",
            "Normal Laptop",
            1000,
            5);

        context.Products.Add(normalStockProduct);

        await context.SaveChangesAsync();

        normalStockProduct.AddStock(
            supplier.Id,
            DateTime.UtcNow.AddDays(30),
            500,
            20);

        await context.SaveChangesAsync();

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.GetLowStockProductsAsync();

        // Assert
        result.Should().ContainSingle();

        result.First().SKU.Should().Be("LOW-001");
    }

    [Fact]
    public async Task GetAllWithBatchesAsync_Should_Return_All_Products_With_Batches()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var supplier = new Supplier(
            "Tech Supplier",
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

        var repository = new ProductRepository(context);

        // Act
        var result = await repository.GetAllWithBatchesAsync();

        // Assert
        result.Should().HaveCount(2);

        result.Should().OnlyContain(p => p.Batches.Any());
    }
}