using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;

namespace Inventory.Infrastructure.Test.Services;

public class UsersReportServiceTests
{
    [Fact]
    public async Task GetCashierSalesAsync_WithEmptyDb_ShouldReturnEmpty()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var service = new UsersReportService(dbContext);

        // Act
        var result = await service.GetCashierSalesAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserStatusBreakdownAsync_WithEmptyDb_ShouldReturnEmpty()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var service = new UsersReportService(dbContext);

        // Act
        var result = await service.GetUserStatusBreakdownAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
