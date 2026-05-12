using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Reports.Inventory;
using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class InventoryReportsControllerTests
{
    private readonly Mock<IInventoryReportService> _inventoryReportServiceMock;

    public InventoryReportsControllerTests()
    {
        _inventoryReportServiceMock = new Mock<IInventoryReportService>();
    }

    private InventoryReportsController CreateController()
    {
        return new InventoryReportsController(_inventoryReportServiceMock.Object);
    }

    [Fact]
    public async Task GetExpiringBatches_ValidRequest_ReturnsOk()
    {
        // Arrange
        var batches = new List<ExpiringBatchDto>();
        _inventoryReportServiceMock
            .Setup(x => x.GetExpiringBatchesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(batches);

        var controller = CreateController();

        // Act
        var result = await controller.GetExpiringBatches(30, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDeadStockProducts_ValidRequest_ReturnsOk()
    {
        // Arrange
        var products = new List<DeadStockDto>();
        _inventoryReportServiceMock
            .Setup(x => x.GetDeadStockProductsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var controller = CreateController();

        // Act
        var result = await controller.GetDeadStockProducts(90, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLowStockProducts_ValidRequest_ReturnsOk()
    {
        // Arrange
        var products = new List<LowStockProductDto>();
        _inventoryReportServiceMock
            .Setup(x => x.GetLowStockProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var controller = CreateController();

        // Act
        var result = await controller.GetLowStockProducts(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetOutOfStockProducts_ValidRequest_ReturnsOk()
    {
        // Arrange
        var products = new List<LowStockProductDto>();
        _inventoryReportServiceMock
            .Setup(x => x.GetOutOfStockProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var controller = CreateController();

        // Act
        var result = await controller.GetOutOfStockProducts(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInventoryTurnover_ValidRequest_ReturnsOk()
    {
        // Arrange
        var turnover = new PagedResult<InventoryTurnoverDto>(new List<InventoryTurnoverDto>(), 1, 20, 0);
        _inventoryReportServiceMock
            .Setup(x => x.GetInventoryTurnoverAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(turnover);

        var controller = CreateController();

        // Act
        var result = await controller.GetInventoryTurnover(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, 1, 20, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}
