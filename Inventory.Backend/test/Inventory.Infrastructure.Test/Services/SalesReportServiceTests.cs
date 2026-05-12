using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Services;

public class SalesReportServiceTests
{
    [Fact]
    public async Task GetSalesSummaryAsync_WithEmptyDb_ShouldReturnZeroes()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        var service = new SalesReportService(dbContext);

        // Act
        var result = await service.GetSalesSummaryAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalOrders);
        Assert.Equal(0, result.TotalRevenue);
    }
}
