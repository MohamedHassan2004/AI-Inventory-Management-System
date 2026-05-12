using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Application.Interfaces;
using Inventory.Domain.Enums;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class PurchaseOrdersControllerTests
{
    private readonly Mock<IPurchaseOrderService> _purchaseOrderServiceMock;
    private readonly Mock<IPurchaseOrderQueryService> _purchaseOrderQueryServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public PurchaseOrdersControllerTests()
    {
        _purchaseOrderServiceMock = new Mock<IPurchaseOrderService>();

        _purchaseOrderQueryServiceMock = new Mock<IPurchaseOrderQueryService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private PurchaseOrdersController CreateController()
    {
        var services = new ServiceCollection();

        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var controller = new PurchaseOrdersController(
            _purchaseOrderServiceMock.Object,
            _purchaseOrderQueryServiceMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            }
        };

        return controller;
    }

    [Fact]
    public async Task Submit_Should_Return_Ok_When_Purchase_Order_Is_Submitted()
    {
        // Arrange
        var dto = new SubmitPurchaseOrderDto
        {
            SupplierId = 1
        };

        var response = new PurchaseOrderResponseDto
        {
            Id = 1,
            SupplierId = 1,
            SupplierName = "Main Supplier",
            Status = PurchaseOrderStatus.Completed
        };

        _purchaseOrderServiceMock
            .Setup(x => x.SubmitAsync(
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.Submit(
            dto,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Should_Return_Ok_When_Purchase_Order_Exists()
    {
        // Arrange
        var response = new PurchaseOrderResponseDto
        {
            Id = 1,
            SupplierId = 1,
            SupplierName = "Main Supplier"
        };

        _purchaseOrderQueryServiceMock
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.GetById(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_Should_Return_Ok_When_Purchase_Orders_Exist()
    {
        // Arrange
        var orders = new List<PurchaseOrderResponseDto>
        {
            new()
            {
                Id = 1,
                SupplierId = 1,
                SupplierName = "Main Supplier"
            }
        };

        var pagedResult = new PagedResult<PurchaseOrderResponseDto>(
            orders,
            1,
            10,
            1);

        _purchaseOrderQueryServiceMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<PurchaseOrderFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(pagedResult));

        var controller = CreateController();

        // Act
        var result = await controller.GetAll(
            new PurchaseOrderFilter(),
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetItemsByPurchaseOrder_Should_Return_Ok_When_Items_Exist()
    {
        // Arrange
        var items = new List<PurchaseOrderItemResponseDto>
        {
            new()
            {
                Id = 1,
                ProductId = 1,
                ProductName = "Laptop",
                Quantity = 5
            }
        };

        _purchaseOrderQueryServiceMock
            .Setup(x => x.GetItemsByPurchaseOrderIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<PurchaseOrderItemResponseDto>>(items));

        var controller = CreateController();

        // Act
        var result = await controller.GetItemsByPurchaseOrder(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}