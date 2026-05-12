using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Reports.Dashboard;
using Inventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
    }

    private DashboardController CreateController()
    {
        return new DashboardController(_dashboardServiceMock.Object);
    }

    [Fact]
    public async Task GetSummary_ValidRequest_ReturnsOk()
    {
        // Arrange
        var summary = new DashboardSummaryDto { TotalPendingOrders = 5 };
        _dashboardServiceMock
            .Setup(x => x.GetSummaryAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var controller = CreateController();

        // Act
        var result = await controller.GetSummary(null, null, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(summary);
    }
}
