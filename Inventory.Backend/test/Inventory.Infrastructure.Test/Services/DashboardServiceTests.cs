using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_WithEmptyDb_ShouldReturnZeroes()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        var service = new DashboardService(dbContext);

        // Act
        var result = await service.GetSummaryAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalProducts);
        Assert.Equal(0, result.LowStockProducts);
    }
}
