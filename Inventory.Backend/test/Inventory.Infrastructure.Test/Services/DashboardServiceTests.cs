using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Services;
using Inventory.Infrastructure.Test.Helpers;
using Xunit;
using System.Threading;
using System;
using System.Threading.Tasks;
using Inventory.Infrastructure.Queries.Reports;

namespace Inventory.Infrastructure.Test.Services;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_WithEmptyDb_ShouldReturnZeroes()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var service = new DashboardQuery(dbContext);

        // Act
        var result = await service.GetSummaryAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalProducts);
        Assert.Equal(0, result.LowStockProducts);
        Assert.Equal(0, result.TotalPendingOrders);
    }
}
