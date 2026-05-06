using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Repositories;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Repositories;

public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_Should_Save_Changes_To_Database()
    {
        // Arrange
        await using var context = DbContextFactory.Create();

        var unitOfWork = new UnitOfWork(context);

        context.Categories.Add(
            new Category(
                "Electronics",
                "image.jpg"));

        // Act
        var result = await unitOfWork.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);

        context.Categories.Should().HaveCount(1);
    }
}