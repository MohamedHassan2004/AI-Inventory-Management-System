using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Reports.Returns;
using Inventory.Application.Interfaces.Queries.Reports;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class ReturnsReportsControllerTests
{
    private readonly Mock<IReturnsReportQuery> _returnsReportServiceMock;

    public ReturnsReportsControllerTests()
    {
        _returnsReportServiceMock = new Mock<IReturnsReportQuery>();
    }

    private ReturnsReportsController CreateController()
    {
        return new ReturnsReportsController(_returnsReportServiceMock.Object);
    }


    [Fact]
    public async Task GetTopReturnedProducts_ValidRequest_ReturnsOk()
    {
        // Arrange
        var products = new List<TopReturnedProductDto>();
        _returnsReportServiceMock
            .Setup(x => x.GetTopReturnedProductsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var controller = CreateController();

        // Act
        var result = await controller.GetTopReturnedProducts(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 5, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
