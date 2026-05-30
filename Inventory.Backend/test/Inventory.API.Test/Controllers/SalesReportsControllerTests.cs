using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Reports.Sales;
using Inventory.Application.Interfaces.Queries.Reports;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class SalesReportsControllerTests
{
    private readonly Mock<ISalesReportQuery> _salesReportServiceMock;

    public SalesReportsControllerTests()
    {
        _salesReportServiceMock = new Mock<ISalesReportQuery>();
    }

    private SalesReportsController CreateController()
    {
        return new SalesReportsController(_salesReportServiceMock.Object);
    }

    [Fact]
    public async Task GetSalesSummary_ValidRequest_ReturnsOk()
    {
        // Arrange
        var summary = new SalesSummaryDto();
        _salesReportServiceMock
            .Setup(x => x.GetSalesSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var controller = CreateController();

        // Act
        var result = await controller.GetSalesSummary(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTopSellingProducts_ValidRequest_ReturnsOk()
    {
        // Arrange
        var products = new List<SalesTopProductDto>();
        _salesReportServiceMock
            .Setup(x => x.GetTopSellingProductsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var controller = CreateController();

        // Act
        var result = await controller.GetTopSellingProducts(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 5, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSalesAnalytics_ValidRequest_ReturnsOk()
    {
        // Arrange
        var analytics = new SalesAnalyticsDto();
        _salesReportServiceMock
            .Setup(x => x.GetSalesAnalyticsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analytics);

        var controller = CreateController();

        // Act
        var result = await controller.GetSalesAnalytics(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProfitMargins_ValidRequest_ReturnsOk()
    {
        // Arrange
        var margins = new PagedResult<ProfitMarginDto>(new List<ProfitMarginDto>(), 1, 20, 0);
        _salesReportServiceMock
            .Setup(x => x.GetProfitMarginsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(margins);

        var controller = CreateController();

        // Act
        var result = await controller.GetProfitMargins(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow, 1, 20, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
