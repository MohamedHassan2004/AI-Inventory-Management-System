using FluentAssertions;
using Inventory.API.Controllers;
using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Queries;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Inventory.API.Test.Controllers;

public class ReturnOrdersControllerTests
{
    private readonly Mock<IReturnOrderService> _returnOrderServiceMock;
    private readonly Mock<IReturnOrderQueryService> _returnOrderQueryServiceMock;
    private readonly Mock<ILocalizationService> _localizationMock;

    public ReturnOrdersControllerTests()
    {
        _returnOrderServiceMock = new Mock<IReturnOrderService>();

        _returnOrderQueryServiceMock = new Mock<IReturnOrderQueryService>();

        _localizationMock = new Mock<ILocalizationService>();
    }

    private ReturnOrdersController CreateController()
    {
        var services = new ServiceCollection();
        services.AddControllers();
        services.AddSingleton(_localizationMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var controller = new ReturnOrdersController(
            _returnOrderServiceMock.Object,
            _returnOrderQueryServiceMock.Object);

        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        "cashier-123")
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
    public async Task Create_ReturnOrderIsCreated_ReturnsOk()
    {
        // Arrange
        var dto = new CreateReturnOrderDto
        {
            OriginalOrderId = 1
        };

        var response = new ReturnOrderResponseDto
        {
            Id = 1,
            OriginalOrderId = 1,
            CashierId = "cashier-123"
        };

        _returnOrderServiceMock
            .Setup(x => x.CreateAsync(
                "cashier-123",
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(response));

        var controller = CreateController();

        // Act
        var result = await controller.Create(
            dto,
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnOrderExists_ReturnsOk()
    {
        // Arrange
        var response = new ReturnOrderResponseDto
        {
            Id = 1,
            OriginalOrderId = 1,
            CashierId = "cashier-123"
        };

        _returnOrderQueryServiceMock
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
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetById_ReturnOrderDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var error = new Error("ReturnOrder.NotFound", "Order not found", ErrorType.NotFound);
        _returnOrderQueryServiceMock
            .Setup(x => x.GetByIdAsync(
                99,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReturnOrderResponseDto>(error));

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
    public async Task Create_ValidationFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateReturnOrderDto { OriginalOrderId = 0 };
        var error = new Error("ReturnOrder.Validation", "Invalid data", ErrorType.Validation);
        
        _returnOrderServiceMock
            .Setup(x => x.CreateAsync(
                "cashier-123",
                dto,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ReturnOrderResponseDto>(error));

        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns("Bad Request");

        var controller = CreateController();

        // Act
        var result = await controller.Create(
            dto,
            CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GetAll_ReturnOrdersExist_ReturnsOk()
    {
        // Arrange
        var orders = new List<ReturnOrderResponseDto>
        {
            new()
            {
                Id = 1,
                OriginalOrderId = 1,
                CashierId = "cashier-123"
            }
        };

        var pagedResult = new PagedResult<ReturnOrderResponseDto>(
            orders,
            1,
            10,
            1);

        _returnOrderQueryServiceMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<ReturnOrderFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(pagedResult));

        var controller = CreateController();

        // Act
        var result = await controller.GetAll(
            new ReturnOrderFilter(),
            CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }
}