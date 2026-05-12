using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Services;

public class InventoryReportServiceTests
{
    [Fact]
    public async Task GetLowStockProductsAsync_WithEmptyDb_ShouldReturnEmpty()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        var service = new InventoryReportService(dbContext);

        // Act
        var result = await service.GetLowStockProductsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
