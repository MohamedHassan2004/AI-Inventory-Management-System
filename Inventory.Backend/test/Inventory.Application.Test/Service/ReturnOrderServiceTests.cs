using FluentAssertions;
using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Inventory.Application.Test.Service;

public class ReturnOrderServiceTests
{
    private readonly Mock<IReturnOrderRepository> _returnOrderRepositoryMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<ReturnOrderService>> _loggerMock = new();
    private readonly Mock<ILocalizationService> _localizationMock = new();

    public ReturnOrderServiceTests()
    {
        _localizationMock
            .Setup(x => x.GetMessage(It.IsAny<string>()))
            .Returns<string>(key => key);
    }

    private ReturnOrderService CreateService() =>
        new(
            _returnOrderRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _localizationMock.Object);

    private Order CreateCompletedOrderWithItem(out Product product, out OrderItem orderItem, decimal quantity = 5m)
    {
        var order = Order.CreateDraft("cashier-1");
        
        product = new Product("SKU-1", "Product", 100m, 10);
        var prodIdProp = typeof(Product).GetProperty("Id");
        prodIdProp?.SetValue(product, 1);
        
        product.AddStock(1, DateTime.UtcNow.AddDays(10), 50m, 10m);
        
        order.AddItem(product, quantity);
        order.Confirm((PaymentMethod)1, (OrderType)1); // Cast to enums
        order.MarkAsDelivered();
        
        var statusProp = typeof(Order).GetProperty("Status");
        statusProp?.SetValue(order, OrderStatus.Completed);
        
        var idProp = typeof(Order).GetProperty("Id");
        idProp?.SetValue(order, 1);

        // Get the order item
        orderItem = order.Items.First();
        var itemIdProp = typeof(OrderItem).GetProperty("Id");
        itemIdProp?.SetValue(orderItem, 1);

        return order;
    }

    [Fact]
    public async Task CreateAsync_ValidReturn_ReplenishesStockSuccessfully()
    {
        // Arrange
        var order = CreateCompletedOrderWithItem(out var product, out var orderItem, 5m);
        
        var dto = new CreateReturnOrderDto
        {
            OriginalOrderId = 1,
            Reason = "Defective",
            Items = new List<CreateReturnOrderItemDto>
            {
                new CreateReturnOrderItemDto
                {
                    OriginalOrderItemId = 1,
                    Quantity = 2m,
                    NewExpiryDate = DateTime.UtcNow.AddDays(5)
                }
            }
        };

        _orderRepositoryMock.Setup(x => x.GetCompletedForReturnAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _mapperMock.Setup(m => m.Map<ReturnOrderResponseDto>(It.IsAny<ReturnOrder>())).Returns(new ReturnOrderResponseDto());

        var service = CreateService();

        // Act
        var result = await service.CreateAsync("cashier-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _returnOrderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ReturnOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        var dto = new CreateReturnOrderDto { OriginalOrderId = 1 };

        _orderRepositoryMock.Setup(x => x.GetCompletedForReturnAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Order)null!);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync("cashier-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_OrderItemNotFound_ReturnsFailure()
    {
        // Arrange
        var order = CreateCompletedOrderWithItem(out _, out _, 5m);
        
        var dto = new CreateReturnOrderDto
        {
            OriginalOrderId = 1,
            Items = new List<CreateReturnOrderItemDto>
            {
                new CreateReturnOrderItemDto
                {
                    OriginalOrderItemId = 99, // Non-existent item
                    Quantity = 2m,
                    NewExpiryDate = DateTime.UtcNow.AddDays(5)
                }
            }
        };

        _orderRepositoryMock.Setup(x => x.GetCompletedForReturnAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync("cashier-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ORDER_ITEM_NOT_FOUND");
    }

    [Fact]
    public async Task CreateAsync_DomainRuleViolation_ReturnsFailure()
    {
        // Arrange
        var order = Order.CreateDraft("cashier-1"); // Not completed
        var idProp = typeof(Order).GetProperty("Id");
        idProp?.SetValue(order, 1);
        
        var dto = new CreateReturnOrderDto { OriginalOrderId = 1 };

        _orderRepositoryMock.Setup(x => x.GetCompletedForReturnAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync("cashier-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("INVALID_OPERATION"); // InvalidOperationException mapped to INVALID_OPERATION
    }

    [Fact]
    public async Task CreateAsync_ValidationException_ReturnsFailure()
    {
        // Arrange
        var order = CreateCompletedOrderWithItem(out _, out _, 5m);
        
        var dto = new CreateReturnOrderDto
        {
            OriginalOrderId = 1,
            Items = new List<CreateReturnOrderItemDto>
            {
                new CreateReturnOrderItemDto
                {
                    OriginalOrderItemId = 1,
                    Quantity = -2m, // Invalid negative quantity (ArgumentException in ReturnOrderItem ctor)
                    NewExpiryDate = DateTime.UtcNow.AddDays(5)
                }
            }
        };

        _orderRepositoryMock.Setup(x => x.GetCompletedForReturnAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync("cashier-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("VALIDATION_ERROR"); // ArgumentException mapped to VALIDATION_ERROR
    }

    [Fact]
    public async Task CreateAsync_ReturnQuantityExceeded_ReturnsFailure()
    {
        // Arrange
        var order = CreateCompletedOrderWithItem(out _, out _, 5m);
        
        var dto = new CreateReturnOrderDto
        {
            OriginalOrderId = 1,
            Items = new List<CreateReturnOrderItemDto>
            {
                new CreateReturnOrderItemDto
                {
                    OriginalOrderItemId = 1,
                    Quantity = 10m, // More than purchased
                    NewExpiryDate = DateTime.UtcNow.AddDays(5)
                }
            }
        };

        _orderRepositoryMock.Setup(x => x.GetCompletedForReturnAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync("cashier-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RETURN_QUANTITY_EXCEEDED");
    }

    [Fact]
    public async Task CreateAsync_DuplicateReturnItem_ReturnsFailure()
    {
        // Arrange
        var order = CreateCompletedOrderWithItem(out _, out _, 5m);
        
        var dto = new CreateReturnOrderDto
        {
            OriginalOrderId = 1,
            Items = new List<CreateReturnOrderItemDto>
            {
                new CreateReturnOrderItemDto
                {
                    OriginalOrderItemId = 1,
                    Quantity = 2m,
                    NewExpiryDate = DateTime.UtcNow.AddDays(5)
                },
                new CreateReturnOrderItemDto // duplicate item
                {
                    OriginalOrderItemId = 1,
                    Quantity = 1m,
                    NewExpiryDate = DateTime.UtcNow.AddDays(5)
                }
            }
        };

        _orderRepositoryMock.Setup(x => x.GetCompletedForReturnAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync("cashier-1", dto, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("DUPLICATE_RETURN_ITEM");
    }
}
