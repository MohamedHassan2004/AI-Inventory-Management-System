using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Domain.Enums;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;
using Inventory.Application.Interfaces.Queries;
using Inventory.Application.Interfaces.Documents;

namespace Inventory.API.Test.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<IOrderQueryService> _orderQueryServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly Mock<IReceiptService> _receiptServiceMock;

    public OrdersControllerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();

        _orderQueryServiceMock = new Mock<IOrderQueryService>();

        _receiptServiceMock = new Mock<IReceiptService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private OrdersController CreateController()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        services.AddSingleton(_localizationMock.Object);

        var serviceProvider = services.BuildServiceProvider();

        var controller = new OrdersController(
            _orderServiceMock.Object,
            _orderQueryServiceMock.Object,
            _receiptServiceMock.Object);

        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        "user-123")
                },
                "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                User = user
            }
        };

        return controller;
    }

    [Fact]
    public async Task Submit_OrderIsSubmitted_ReturnsOk()
    {
        // Arrange
        var dto = new SubmitOrderDto
        {
            PaymentMethod = PaymentMethod.Cash,
            OrderType = OrderType.InStore
        };

        var response = new DetailedOrderResponseDto
        {
            Id = 1,
            CashierId = "user-123"
        };

        _orderServiceMock
            .Setup(x => x.SubmitAsync(
                "user-123",
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

        var okResult = result as OkObjectResult;

        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateDraft_DraftIsCreated_ReturnsOk()
    {
        // Arrange
        var response = new DetailedOrderResponseDto
        {
            Id = 1,
            CashierId = "user-123",
            Status = OrderStatus.Draft
        };

        _orderServiceMock
            .Setup(x => x.CreateDraftAsync(
                "user-123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.CreateDraft(
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_OrderExists_ReturnsOk()
    {
        // Arrange
        var response = new DetailedOrderResponseDto
        {
            Id = 1,
            CashierId = "user-123"
        };

        _orderQueryServiceMock
            .Setup(x => x.GetByIdAsync(
                "user-123",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.GetById(
            1,
            CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetById_OrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var error = new Error("Order.NotFound", "Order not found", ErrorType.NotFound);
        _orderQueryServiceMock
            .Setup(x => x.GetByIdAsync(
                "user-123",
                99,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DetailedOrderResponseDto>(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Not Found");

        var controller = CreateController();

        // Act
        var result = await controller.GetById(
            99,
            CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GetById_DraftOrderBelongsToAnotherCashier_ReturnsForbidden()
    {
        // Arrange
        var error = new Error(
            "DRAFT_ORDER_ACCESS_DENIED",
            "Draft order access denied",
            ErrorType.Forbidden);

        _orderQueryServiceMock
            .Setup(x => x.GetByIdAsync(
                "user-123",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DetailedOrderResponseDto>(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Forbidden");

        var controller = CreateController();

        // Act
        var result = await controller.GetById(
            1,
            CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Submit_ValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new SubmitOrderDto();
        var error = new Error("Order.Validation", "Invalid data", ErrorType.Validation);
        
        _orderServiceMock
            .Setup(x => x.SubmitAsync(
                "user-123",
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DetailedOrderResponseDto>(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Bad Request");

        var controller = CreateController();

        // Act
        var result = await controller.Submit(
            dto,
            CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CancelDraft_CancelSucceeds_ReturnsNoContent()
    {
        // Arrange
        _orderServiceMock
            .Setup(x => x.CancelDraftAsync(
                "user-123",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var controller = CreateController();

        // Act
        var result = await controller.CancelDraft(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelDraft_OrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Not Found");

        _orderServiceMock
            .Setup(x => x.CancelDraftAsync(
                "user-123",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(
                new Error(
                    "NOT_FOUND",
                    "Order not found",
                    ErrorType.NotFound)));

        var controller = CreateController();

        // Act
        var result = await controller.CancelDraft(
            1,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<ObjectResult>();

        var objectResult = result as ObjectResult;

        objectResult!.StatusCode.Should().Be(404);
    }
}
