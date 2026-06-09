using FluentAssertions;
using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Queries;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
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

    private OrderService CreateService() =>
        new(
            _orderRepositoryMock.Object,
            _orderQueryServiceMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _localizationMock.Object);

    #region CreateDraftAsync Tests

    [Fact]
    public async Task CreateDraftAsync_ValidCashier_CreatesDraftSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var cashierId = "cashier-1";

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto { CashierId = cashierId, Status = OrderStatus.Draft });

        // Act
        var result = await service.CreateDraftAsync(cashierId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CashierId.Should().Be(cashierId);
        result.Value.Status.Should().Be(OrderStatus.Draft);

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region AddItemAsync Tests

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
    }

    [Fact]
    public async Task AddItemAsync_DraftOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        var service = CreateService();

        // Act
        var result = await service.AddItemAsync("cashier-1", 1, new AddOrderItemDto { SKU = "DUMMY", Quantity = 1 }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ORDER_NOT_FOUND_OR_NOT_DRAFT");
    }

    [Fact]
    public async Task AddItemAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepositoryMock
            .Setup(x => x.GetBySkuWithBatchesAsync("SKU-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product)null!);

        var service = CreateService();

        // Act
        var result = await service.AddItemAsync("cashier-1", 1, new AddOrderItemDto { SKU = "SKU-1", Quantity = 1 }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task AddItemAsync_ValidProduct_AddsItemSuccessfully()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var idProperty = product.GetType().GetProperty("Id");
        idProperty?.SetValue(product, 10);
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 10); // add 10 items to stock

        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepositoryMock
            .Setup(x => x.GetBySkuWithBatchesAsync("SKU-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.AddItemAsync("cashier-1", 1, new AddOrderItemDto { SKU = "SKU-1", Quantity = 2 }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RemoveItemAsync Tests

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
        var result = await service.RemoveItemAsync("other-cashier", 1, 10, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
    }

    [Fact]
    public async Task RemoveItemAsync_ItemNotFound_ReturnsFailure()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        // No items added to draft order

        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.RemoveItemAsync("cashier-1", 1, 10, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("VALIDATION_ERROR"); // Throws InvalidOperationException / ArgumentException internally in Order
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_RemovesItemSuccessfully()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var idProperty = product.GetType().GetProperty("Id");
        idProperty?.SetValue(product, 10);
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 10);

        order.AddItem(product, 2);

        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.RemoveItemAsync("cashier-1", 1, 10, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateItemQuantityAsync Tests

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
        var result = await service.UpdateItemQuantityAsync("other-cashier", 1, 10, 2, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var idProperty = product.GetType().GetProperty("Id");
        idProperty?.SetValue(product, 10);
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 5); // Only 5 in stock

        order.AddItem(product, 2);

        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.UpdateItemQuantityAsync("cashier-1", 1, 10, 10, CancellationToken.None); // request 10

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("VALIDATION_ERROR"); 
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_ValidQuantity_UpdatesSuccessfully()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var idProperty = product.GetType().GetProperty("Id");
        idProperty?.SetValue(product, 10);
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 10); 

        order.AddItem(product, 2);

        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.UpdateItemQuantityAsync("cashier-1", 1, 10, 5, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ConfirmOrderAsync Tests

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
        var result = await service.ConfirmOrderAsync("other-cashier", 1, new ConfirmOrderDto(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_DraftOrderNotFound_ReturnsFailureAndRollbacks()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.GetDraftForConfirmationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        var service = CreateService();

        // Act
        var result = await service.ConfirmOrderAsync("cashier-1", 1, new ConfirmOrderDto(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ORDER_NOT_FOUND_OR_NOT_DRAFT");
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_EmptyOrder_ReturnsFailureAndRollbacks()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForConfirmationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.ConfirmOrderAsync("cashier-1", 1, new ConfirmOrderDto(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EMPTY_ORDER");
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmOrderAsync_ValidDraftOrder_ConfirmsSuccessfullyAndCommitsTransaction()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var idProperty = product.GetType().GetProperty("Id");
        idProperty?.SetValue(product, 10);
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 10);
        
        order.AddItem(product, 2);

        _orderRepositoryMock
            .Setup(x => x.GetDraftForConfirmationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.ConfirmOrderAsync("cashier-1", 1, new ConfirmOrderDto { OrderType = OrderType.InStore, PaymentMethod = PaymentMethod.Cash }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Completed);
        
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CancelDraftAsync Tests

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
        var result = await service.CancelDraftAsync("other-cashier", 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DRAFT_ORDER_ACCESS_DENIED");
    }

    [Fact]
    public async Task CancelDraftAsync_DraftOrderNotFound_ReturnsFailure()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        var service = CreateService();

        // Act
        var result = await service.CancelDraftAsync("cashier-1", 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ORDER_NOT_FOUND_OR_NOT_DRAFT");
    }

    [Fact]
    public async Task CancelDraftAsync_ValidDraftOrder_CancelsSuccessfully()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.CancelDraftAsync("cashier-1", 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region SubmitAsync (Legacy Workflow) Tests

    [Fact]
    public async Task SubmitAsync_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new SubmitOrderDto 
        { 
            Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } } 
        };
        
        _productRepositoryMock
            .Setup(x => x.GetWithBatchesListAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>()); // returns empty, product not found

        var service = CreateService();

        // Act
        var result = await service.SubmitAsync("user-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("PRODUCT_NOT_FOUND");
    }

    [Fact]
    public async Task SubmitAsync_EmptyOrder_ReturnsFailure()
    {
        // Arrange
        var dto = new SubmitOrderDto { Items = new List<OrderItemDto>() };
        
        _productRepositoryMock
            .Setup(x => x.GetWithBatchesListAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var service = CreateService();

        // Act
        var result = await service.SubmitAsync("user-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("EMPTY_ORDER");
    }

    [Fact]
    public async Task SubmitAsync_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var idProperty = product.GetType().GetProperty("Id");
        idProperty?.SetValue(product, 1);
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 1); // Only 1 in stock

        var dto = new SubmitOrderDto 
        { 
            Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } } 
        };
        
        _productRepositoryMock
            .Setup(x => x.GetWithBatchesListAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var service = CreateService();

        // Act
        var result = await service.SubmitAsync("user-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INSUFFICIENT_STOCK");
    }

    [Fact]
    public async Task SubmitAsync_ValidOrder_SavesOrderAndReturnsSuccess()
    {
        // Arrange
        var product = new Product("SKU-1", "Product 1", 100m, 10);
        var idProperty = product.GetType().GetProperty("Id");
        idProperty?.SetValue(product, 1);
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 10);

        var dto = new SubmitOrderDto 
        { 
            Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } },
            PaymentMethod = PaymentMethod.Cash,
            OrderType = OrderType.InStore
        };
        
        _productRepositoryMock
            .Setup(x => x.GetWithBatchesListAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.SubmitAsync("user-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ApplyDiscountAsync Tests

    [Fact]
    public async Task ApplyDiscountAsync_ValidDiscount_AppliesDiscountSuccessfully()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        _orderRepositoryMock
            .Setup(x => x.GetDraftForMutationAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.ApplyDiscountAsync("cashier-1", 1, new ApplyDiscountDto { DiscountPercentage = 10 }, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.DiscountPercentage.Should().Be(10);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region MarkAsDeliveredAsync Tests

    [Fact]
    public async Task MarkAsDeliveredAsync_ValidOrder_MarksAsDeliveredSuccessfully()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        // We need an order OutForDelivery. We can't transition it fully cleanly here without mocking state, 
        // but let's assume we can change the status via reflection or it allows MarkAsDelivered directly 
        // if we set its state properly.
        var statusProperty = order.GetType().GetProperty("Status");
        statusProperty?.SetValue(order, OrderStatus.OutForDelivery);

        _orderRepositoryMock
            .Setup(x => x.GetOutForDeliveryForStatusChangeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
            
        _orderRepositoryMock
            .Setup(x => x.GetForDetailedResponseAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.MarkAsDeliveredAsync("cashier-1", 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Completed);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsDeliveredAsync_OrderNotOutForDelivery_ReturnsFailure()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.GetOutForDeliveryForStatusChangeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        var service = CreateService();

        // Act
        var result = await service.MarkAsDeliveredAsync("cashier-1", 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ORDER_NOT_OUT_FOR_DELIVERY");
    }

    #endregion

    #region FailDeliveryAsync Tests

    [Fact]
    public async Task FailDeliveryAsync_ValidOrder_FailsDeliverySuccessfully()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1");
        var statusProperty = order.GetType().GetProperty("Status");
        statusProperty?.SetValue(order, OrderStatus.OutForDelivery);

        _orderRepositoryMock
            .Setup(x => x.GetOutForDeliveryForRestockAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
            
        _orderRepositoryMock
            .Setup(x => x.GetForDetailedResponseAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mapperMock.Setup(m => m.Map<DetailedOrderResponseDto>(It.IsAny<Order>()))
            .Returns(new DetailedOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.FailDeliveryAsync("cashier-1", 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FailDeliveryAsync_OrderNotOutForDelivery_ReturnsFailureAndRollbacks()
    {
        // Arrange
        _orderRepositoryMock
            .Setup(x => x.GetOutForDeliveryForRestockAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order)null!);

        var service = CreateService();

        // Act
        var result = await service.FailDeliveryAsync("cashier-1", 1, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ORDER_NOT_OUT_FOR_DELIVERY");
        
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
