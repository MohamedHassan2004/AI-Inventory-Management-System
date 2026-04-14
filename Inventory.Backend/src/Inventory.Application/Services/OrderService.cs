using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly ILocalizationService _localizationService;

        public OrderService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<OrderService> logger,
            ILocalizationService localizationService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _localizationService = localizationService;
        }

        public async Task<Result<int>> CreateAsync(CreateOrderDto dto, string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating order for user {UserId}", userId);

            if (dto.Items == null || !dto.Items.Any())
            {
                _logger.LogWarning("Attempt to create empty order");
                return Result.Failure<int>(new Error(
                    "EMPTY_ORDER",
                    _localizationService.GetMessage("EmptyOrder"),
                    ErrorType.Validation));
            }

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

            foreach (var item in dto.Items)
            {
                var product = await _productRepository.GetWithBatchesAsync(item.ProductId, cancellationToken);

                if (product == null)
                {
                    _logger.LogWarning("Product not found with Id: {ProductId}", item.ProductId);
                    return Result.Failure<int>(new Error(
                        "PRODUCT_NOT_FOUND",
                        _localizationService.GetMessage("ProductNotFound"),
                        ErrorType.NotFound));
                }

                try
                {
                    var orderItem = new OrderItem(product, item.Quantity);
                    order.AddItem(orderItem);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error adding item to order");
                    return Result.Failure<int>(new Error(
                        "INVALID_ITEM",
                        _localizationService.GetMessage("InvalidOrderItem"),
                        ErrorType.Validation));
                }
            }

            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order created successfully with Id: {OrderId}", order.Id);

            return Result.Success(order.Id);
        }

        public async Task<Result> ApplyDiscountAsync(int orderId, decimal discount, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Applying discount {Discount} to order {OrderId}", discount, orderId);

            var order = await _orderRepository.GetByIdWithItemsAsync(orderId);

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid discount for order {OrderId}", orderId);
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

            var order = await _orderRepository.GetByIdWithItemsAsync(orderId);

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
                order.CompleteOrder(dto.PaymentMethod, dto.OrderType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error completing order {OrderId}", orderId);
                return Result.Failure(new Error(
                    "INVALID_OPERATION",
                    _localizationService.GetMessage("InvalidOrderOperation"),
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

            var order = await _orderRepository.GetByIdWithItemsAndProductsAsync(orderId);

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cancelling order {OrderId}", orderId);
                return Result.Failure(new Error(
                    "INVALID_OPERATION",
                    _localizationService.GetMessage("InvalidOrderOperation"),
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

            var order = await _orderRepository.GetByIdWithItemsAndProductsAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found {OrderId}", orderId);
                return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("OrderNotFound"), ErrorType.NotFound));
            }

            var product = await _productRepository.GetWithBatchesAsync(dto.ProductId, cancellationToken);

            if (product == null)
            {
                _logger.LogWarning("Product not found {ProductId}", dto.ProductId);
                return Result.Failure(new Error("PRODUCT_NOT_FOUND", _localizationService.GetMessage("ProductNotFound"), ErrorType.NotFound));
            }

            try
            {
                var item = new OrderItem(product, dto.Quantity);
                order.AddItem(item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adding item to order");
                return Result.Failure(new Error(
                    "INVALID_ITEM",
                    _localizationService.GetMessage("InvalidOrderItem"),
                    ErrorType.Validation));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        public async Task<Result> UpdateItemQuantityAsync(int orderId, int itemId, decimal quantity, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating item {ItemId} in order {OrderId}", itemId, orderId);

            var order = await _orderRepository.GetByIdWithItemsAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found {OrderId}", orderId);
                return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("OrderNotFound"), ErrorType.NotFound));
            }

            try
            {
                order.UpdateItemQuantity(itemId, quantity);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating item {ItemId} in order {OrderId}", itemId, orderId);
                return Result.Failure(new Error("INVALID_OPERATION",_localizationService.GetMessage("InvalidOrderOperation"), ErrorType.Validation));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        public async Task<Result> RemoveItemAsync(int orderId, int itemId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Removing item {ItemId} from order {OrderId}", itemId, orderId);

            var order = await _orderRepository.GetByIdWithItemsAsync(orderId);

            if (order == null)
            {
                _logger.LogWarning("Order not found {OrderId}", orderId);
                return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("OrderNotFound"), ErrorType.NotFound));
            }

            try
            {
                order.RemoveItem(itemId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error removing item {ItemId} from order {OrderId}", itemId, orderId);
                return Result.Failure(new Error("INVALID_OPERATION",_localizationService.GetMessage("InvalidOrderOperation"),ErrorType.Validation));
            }

            _orderRepository.Update(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

    }
}