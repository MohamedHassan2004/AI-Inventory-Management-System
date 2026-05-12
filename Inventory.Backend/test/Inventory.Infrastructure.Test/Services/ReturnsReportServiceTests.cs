using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Services;

public class ReturnsReportServiceTests
{
    [Fact]
    public async Task GetReturnsSummaryAsync_WithEmptyDb_ShouldReturnZeroes()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        var service = new ReturnsReportService(dbContext);

        // Act
        var result = await service.GetReturnsSummaryAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalReturns);
        Assert.Equal(0, result.TotalRefundAmount);
    }
}
