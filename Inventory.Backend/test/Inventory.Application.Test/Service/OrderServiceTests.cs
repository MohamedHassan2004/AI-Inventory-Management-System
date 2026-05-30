using FluentAssertions;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Queries;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Inventory.Application.Test.Service;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IOrderQueryService> _orderQueryServiceMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<OrderService>> _loggerMock = new();
    private readonly Mock<ILocalizationService> _localizationMock = new();

    public OrderServiceTests()
    {
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns<string>(key => key);
    }

    [Fact]
    public async Task AddItemAsync_Should_Return_Forbidden_When_Draft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        var order = Order.CreateDraft("owner-cashier");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.AddItemAsync(
            "other-cashier",
            1,
            new AddOrderItemDto { SKU = "P-001", Quantity = 1 },
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
        result.Error.Type.Should().Be(ErrorType.Forbidden);

        _productRepositoryMock.Verify(
            x => x.GetBySkuWithBatchesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveItemAsync_Should_Return_Forbidden_When_Draft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        var order = Order.CreateDraft("owner-cashier");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.RemoveItemAsync(
            "other-cashier",
            1,
            10,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
        result.Error.Type.Should().Be(ErrorType.Forbidden);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_Should_Return_Forbidden_When_Draft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        var order = Order.CreateDraft("owner-cashier");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.UpdateItemQuantityAsync(
            "other-cashier",
            1,
            10,
            2,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
        result.Error.Type.Should().Be(ErrorType.Forbidden);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmOrderAsync_Should_Return_Forbidden_And_Roll_Back_When_Draft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        var order = Order.CreateDraft("owner-cashier");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForConfirmationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.ConfirmOrderAsync(
            "other-cashier",
            1,
            new ConfirmOrderDto(),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
        result.Error.Type.Should().Be(ErrorType.Forbidden);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelDraftAsync_Should_Return_Forbidden_When_Draft_Order_Belongs_To_Another_Cashier()
    {
        // Arrange
        var order = Order.CreateDraft("owner-cashier");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.CancelDraftAsync(
            "other-cashier",
            1,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
        result.Error.Type.Should().Be(ErrorType.Forbidden);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private OrderService CreateService() =>
        new(
            _orderRepositoryMock.Object,
            _orderQueryServiceMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _localizationMock.Object);
}
