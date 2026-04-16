using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using Inventory.Domain.Exceptions;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderQueryService _orderQueryService;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly ILocalizationService _localizationService;

        public OrderService(
            IOrderRepository orderRepository,
            IOrderQueryService orderQueryService,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<OrderService> logger,
            ILocalizationService localizationService)
        {
            _orderRepository = orderRepository;
            _orderQueryService = orderQueryService;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _localizationService = localizationService;
        }

        public async Task<Result<int>> CreateAsync(string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating order for user {UserId}", userId);

            Order order;
            
            try
            {
                order = new Order(userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid order creation data");
                return Result.Failure<int>(new Error(
                    "INVALID_ORDER",
                    _localizationService.GetMessage("InvalidOrder"),
                    ErrorType.Validation));
            }

            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order created successfully with Id: {OrderId}", order.Id);

            return Result.Success(order.Id);
        }

        public async Task<Result> ApplyDiscountAsync(int orderId, decimal discount, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Applying discount {Discount} to order {OrderId}", discount, orderId);

            var order = await _orderRepository.GetOrderWithItemsAsync(orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found with Id: {OrderId}", orderId);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("OrderNotFound"),
                    ErrorType.NotFound));
            }

            try
            {
                order.ApplyDiscount(discount);
            }
            catch (InvalidDiscountException ex)
            {
                _logger.LogWarning(ex, "Invalid discount for order {OrderId}. Provided: {Discount}", orderId, discount);
                return Result.Failure(new Error(
                    "INVALID_DISCOUNT",
                    _localizationService.GetMessage("InvalidDiscount"),
                    ErrorType.Validation));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Discount applied successfully to order {OrderId}", orderId);

            return Result.Success();
        }

        public async Task<Result> CompleteAsync(int orderId, CompleteOrderDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Completing order {OrderId}", orderId);

            var order = await _orderRepository.GetOrderWithItemsAsync(orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found with Id: {OrderId}", orderId);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("OrderNotFound"),
                    ErrorType.NotFound));
            }

            try
            {
                order.Complete(dto.PaymentMethod, dto.OrderType);
            }
            catch (EmptyOrderException ex)
            {
                _logger.LogWarning(ex, "Cannot complete empty order {OrderId}", orderId);
                return Result.Failure(new Error(
                    "EMPTY_ORDER",
                    _localizationService.GetMessage("EmptyOrder"),
                    ErrorType.Validation));
            }
            catch (OrderNotEditableException ex)
            {
                _logger.LogWarning(ex, "Error adding item to order");

                return Result.Failure<int>(new Error(
                    "INVALID_ORDER_STATUS",
                    ex.Message,
                    ErrorType.Validation));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} completed successfully", orderId);

            return Result.Success();
        }

        public async Task<Result> CancelAsync(int orderId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Cancelling order {OrderId}", orderId);

            var order = await _orderRepository.GetFullOrderAsync(orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found with Id: {OrderId}", orderId);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("OrderNotFound"),
                    ErrorType.NotFound));
            }

            try
            {
                order.Cancel();
            }
            catch (OrderNotEditableException ex)
            {
                _logger.LogWarning(ex, "Error adding item to order");

                return Result.Failure<int>(new Error(
                    "INVALID_ORDER_STATUS",
                    ex.Message,
                    ErrorType.Validation));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);

            return Result.Success();
        }
        public async Task<Result> AddItemAsync(int orderId, OrderItemDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Adding item to order {OrderId}", orderId);

            var order = await _orderRepository.GetFullOrderAsync(orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found {OrderId}", orderId);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("OrderNotFound"),
                    ErrorType.NotFound));
            }

            var product = await _productRepository.GetWithBatchesAsync(dto.ProductId, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product not found {ProductId}", dto.ProductId);
                return Result.Failure(new Error(
                    "PRODUCT_NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound"),
                    ErrorType.NotFound));
            }

            try
            {
                order.AddItem(product, dto.Quantity);
            }
            catch (InsufficientStockException ex)
            {
                _logger.LogWarning(ex, "Insufficient stock for product {ProductId}", dto.ProductId);
                return Result.Failure(new Error(
                    "INSUFFICIENT_STOCK",
                    _localizationService.GetMessage("InsufficientStock"),
                    ErrorType.Validation));
            }
            catch (OrderNotEditableException ex)
            {
                _logger.LogWarning(ex, "Error adding item to order");

                return Result.Failure<int>(new Error(
                    "INVALID_ORDER_STATUS",
                    ex.Message,
                    ErrorType.Validation));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> UpdateItemQuantityAsync(int orderId, int itemId, decimal quantity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating item {ItemId} in order {OrderId}", itemId, orderId);

            var order = await _orderRepository.GetFullOrderAsync(orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found {OrderId}", orderId);
                return Result.Failure(new Error(
                    "NOT_FOUND",
                    _localizationService.GetMessage("OrderNotFound"),
                    ErrorType.NotFound));
            }

            try
            {
                order.UpdateQuantity(itemId, quantity);
            }
            catch (InsufficientStockException ex)
            {
                _logger.LogWarning(ex, "Insufficient stock while updating item {ItemId}", itemId);
                return Result.Failure(new Error(
                    "INSUFFICIENT_STOCK",
                    _localizationService.GetMessage("InsufficientStock"),
                    ErrorType.Validation));
            }
            catch (OrderNotEditableException ex)
            {
                _logger.LogWarning(ex, "Error adding item to order");

                return Result.Failure<int>(new Error(
                    "INVALID_ORDER_STATUS",
                    ex.Message,
                    ErrorType.Validation));
            }
            catch (OrderItemNotFoundException ex)
            {
                _logger.LogWarning(ex, "Item {ItemId} not found in order {OrderId}", itemId, orderId);
                return Result.Failure(new Error(
                    "ORDER_ITEM_NOT_FOUND",
                    _localizationService.GetMessage("OrderItemNotFound"),
                    ErrorType.NotFound));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Item {ItemId} updated successfully in order {OrderId}", itemId, orderId);

            return Result.Success();
        }
        public async Task<Result> RemoveItemAsync(int orderId, int itemId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Removing item {ItemId} from order {OrderId}", itemId, orderId);

            var order = await _orderRepository.GetFullOrderAsync(orderId, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning("Order not found {OrderId}", orderId);
                return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("OrderNotFound"), ErrorType.NotFound));
            }

            try
            {
                order.RemoveItem(itemId);
            }
            catch (OrderNotEditableException ex)
            {
                _logger.LogWarning(ex, "Error adding item to order");

                return Result.Failure<int>(new Error(
                    "INVALID_ORDER_STATUS",
                    ex.Message,
                    ErrorType.Validation));
            }
            catch (OrderItemNotFoundException ex)
            {
                _logger.LogWarning(ex, "Item {ItemId} not found in order {OrderId}", itemId, orderId);
                return Result.Failure(new Error(
                    "ORDER_ITEM_NOT_FOUND",
                    _localizationService.GetMessage("OrderItemNotFound"),
                    ErrorType.NotFound));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Item {ItemId} removed successfully from order {OrderId}", itemId, orderId);

            return Result.Success();
        }

    }
}