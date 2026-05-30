using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Reports.Users;
using Inventory.Application.Interfaces.Queries.Reports;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class UsersReportsControllerTests
{
    private readonly Mock<IUsersReportQuery> _usersReportServiceMock;

    public UsersReportsControllerTests()
    {
        _usersReportServiceMock = new Mock<IUsersReportQuery>();
    }

    private UsersReportsController CreateController()
    {
        return new UsersReportsController(_usersReportServiceMock.Object);
    }

    [Fact]
    public async Task GetCashierSales_ValidRequest_ReturnsOk()
    {
        // Arrange
        var sales = new List<CashierSalesDto>();
        _usersReportServiceMock
            .Setup(x => x.GetCashierSalesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var controller = CreateController();

        // Act
        var result = await controller.GetCashierSales(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStatusBreakdown_ValidRequest_ReturnsOk()
    {
        // Arrange
        var breakdown = new List<UserStatusBreakdownDto>();
        _usersReportServiceMock
            .Setup(x => x.GetUserStatusBreakdownAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(breakdown);

        var controller = CreateController();

        // Act
        var result = await controller.GetStatusBreakdown(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
