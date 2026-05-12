using FluentAssertions;
using Inventory.Application.DTOs.Reports.Returns;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Inventory.Infrastructure.Tests.Services;

public class ReturnsReportServiceTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }


    [Fact]
    public async Task GetTopReturnedProductsAsync_WithNoReturns_ShouldReturnEmpty()
    {
        // Arrange
        using var dbContext = CreateDbContext();

        var service = new ReturnsReportService(dbContext);

        // Act
        var result = await service.GetTopReturnedProductsAsync(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow,
            10,
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}