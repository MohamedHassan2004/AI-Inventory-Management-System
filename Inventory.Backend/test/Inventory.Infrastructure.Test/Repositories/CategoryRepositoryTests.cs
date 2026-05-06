using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class CategoryRepositoryTests
{
    [Fact]
    public async Task ExistsByNameAsync_Should_Return_True_When_Name_Exists()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Categories.Add(
            new Category("Electronics", "image.jpg"));

        await context.SaveChangesAsync();

        var repository = new CategoryRepository(context);

        // Act
        var result = await repository.ExistsByNameAsync("Electronics");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_Should_Return_False_When_Name_Does_Not_Exist()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        context.Categories.Add(
            new Category("Electronics", "image.jpg"));

        await context.SaveChangesAsync();

        var repository = new CategoryRepository(context);

        // Act
        var result = await repository.ExistsByNameAsync("Furniture");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsByNameAsync_Should_Ignore_Excluded_Id()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var category = new Category(
            "Electronics",
            "image.jpg");

        context.Categories.Add(category);

        await context.SaveChangesAsync();

        var repository = new CategoryRepository(context);

        // Act
        var result = await repository.ExistsByNameAsync(
            "Electronics",
            category.Id);

        // Assert
        result.Should().BeFalse();
    }
}