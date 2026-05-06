using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class RepositoryTests
{
    [Fact]
    public async Task GetAllAsync_Should_Return_All_Entities()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Categories.AddRange(
            new Category("Electronics", "image1.jpg"),
            new Category("Furniture", "image2.jpg"));

        await context.SaveChangesAsync();

        var repository = new Repository<Category>(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Entity_When_Entity_Exists()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var category = new Category(
            "Electronics",
            "image.jpg");

        context.Categories.Add(category);

        await context.SaveChangesAsync();

        var repository = new Repository<Category>(context);

        // Act
        var result = await repository.GetByIdAsync(category.Id);

        // Assert
        result.Should().NotBeNull();

        result!.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Entity_Does_Not_Exist()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var repository = new Repository<Category>(context);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }
}