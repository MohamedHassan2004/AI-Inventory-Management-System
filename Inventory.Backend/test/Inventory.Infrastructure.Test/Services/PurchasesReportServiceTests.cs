using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Services;

public class PurchasesReportServiceTests
{
    [Fact]
    public async Task GetPurchasesSummaryAsync_WithEmptyDb_ShouldReturnZeroes()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        var service = new PurchasesReportService(dbContext);

        // Act
        var result = await service.GetPurchasesSummaryAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalPurchaseOrders);
        Assert.Equal(0, result.TotalPurchaseCost);
    }
}
