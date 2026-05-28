using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using Inventory.Domain.Exceptions;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Enums;

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

        // ─────────────────────────────────────────────────────────────
        //  SUBMIT — single-transaction order creation (LEGACY)
        // ─────────────────────────────────────────────────────────────

        public async Task<Result<DetailedOrderResponseDto>> SubmitAsync(
            string userId,
            SubmitOrderDto dto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Submitting order for user {UserId} with {ItemCount} items", userId, dto.Items.Count);

            // 1. Load all required products with batches in a single query
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = (await _productRepository.GetWithBatchesListAsync(productIds, cancellationToken)).ToList();

            // 2. Validate all products exist
            var missingIds = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missingIds.Any())
            {
                _logger.LogWarning("Products not found: {MissingIds}", string.Join(", ", missingIds));
                return Result.Failure<DetailedOrderResponseDto>(new Error(
                    "PRODUCT_NOT_FOUND",
                    _localizationService.GetMessage("ProductNotFound"),
                    ErrorType.NotFound));
            }

            // 3. Aggregate duplicate product entries and build (product, quantity) pairs
            var productMap = products.ToDictionary(p => p.Id);
            var itemPairs = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => (
                    product: productMap[g.Key],
                    quantity: g.Sum(i => i.Quantity)
                ))
                .ToList()
                .AsReadOnly();

            // 4. Create completed order — stock consumed in-memory via FEFO
            Order order;
            try
            {
                order = Order.Submit(userId, itemPairs, dto.PaymentMethod, dto.OrderType, dto.DiscountPercentage);
            }
            catch (InsufficientStockException ex)
            {
                _logger.LogWarning(ex, "Insufficient stock while submitting order for user {UserId}", userId);
                return Result.Failure<DetailedOrderResponseDto>(new Error(
                    "INSUFFICIENT_STOCK",
                    _localizationService.GetMessage("InsufficientStock"),
                    ErrorType.Validation));
            }
            catch (InvalidDiscountException ex)
            {
                _logger.LogWarning(ex, "Invalid discount {Discount} for order", dto.DiscountPercentage);
                return Result.Failure<DetailedOrderResponseDto>(new Error(
                    "INVALID_DISCOUNT",
                    _localizationService.GetMessage("InvalidDiscount"),
                    ErrorType.Validation));
            }
            catch (EmptyOrderException ex)
            {
                _logger.LogWarning(ex, "Attempted to submit empty order");
                return Result.Failure<DetailedOrderResponseDto>(new Error(
                    "EMPTY_ORDER",
                    _localizationService.GetMessage("EmptyOrder"),
                    ErrorType.Validation));
            }

            // 5. Persist — single SaveChanges saves Order, Items, Consumptions, and batch updates
            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} submitted successfully for user {UserId}", order.Id, userId);

            var response = _mapper.Map<DetailedOrderResponseDto>(order);
            return Result.Success(response);
        }

        // ─────────────────────────────────────────────────────────────
        //  DRAFT WORKFLOW
        // ─────────────────────────────────────────────────────────────

        public async Task<Result<OrderResponseDto>> CreateDraftAsync(string cashierId, CancellationToken cancellationToken = default)
        {
            var order = Order.CreateDraft(cashierId);
            
            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<OrderResponseDto>(order);
            return Result.Success(response);
        }

        public async Task<Result<OrderResponseDto>> AddItemAsync(string cashierId, int orderId, AddOrderItemDto dto, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetDraftByIdAsync(orderId, cancellationToken);
            if (order == null || order.Status != OrderStatus.Draft)
            {
                return Result.Failure<OrderResponseDto>(new Error("ORDER_NOT_FOUND_OR_NOT_DRAFT", "Draft order not found.", ErrorType.NotFound));
            }

            var product = await _productRepository.GetBySkuWithBatchesAsync(dto.SKU, cancellationToken);
            if (product == null)
            {
                 return Result.Failure<OrderResponseDto>(new Error("PRODUCT_NOT_FOUND", "Product not found.", ErrorType.NotFound));
            }

            try
            {
                order.AddItem(product, dto.Quantity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch(InsufficientStockException ex)
            {
                _logger.LogWarning(ex, "Insufficient stock while adding product {ProductId} to order {OrderId}", product.Id, orderId);
                return Result.Failure<OrderResponseDto>(new Error("INSUFFICIENT_STOCK", ex.Message, ErrorType.Validation));
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while adding item to order {OrderId}", orderId);
                return Result.Failure<OrderResponseDto>(new Error("CONCURRENCY_CONFLICT", "Order was modified by another request.", ErrorType.Conflict));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidDiscountException || ex is InvalidOperationException)
            {
                 return Result.Failure<OrderResponseDto>(new Error("VALIDATION_ERROR", ex.Message, ErrorType.Validation));
            }

            var response = _mapper.Map<OrderResponseDto>(order);
            return Result.Success(response);
        }

        public async Task<Result<OrderResponseDto>> RemoveItemAsync(string cashierId, int orderId, int productId, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetDraftByIdAsync(orderId, cancellationToken);
            if (order == null || order.Status != OrderStatus.Draft)
            {
                return Result.Failure<OrderResponseDto>(new Error("ORDER_NOT_FOUND_OR_NOT_DRAFT", "Draft order not found.", ErrorType.NotFound));
            }

            try
            {
                order.RemoveItem(productId);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while removing item from order {OrderId}", orderId);
                return Result.Failure<OrderResponseDto>(new Error("CONCURRENCY_CONFLICT", "Order was modified by another request.", ErrorType.Conflict));
            }
            catch (InvalidOperationException ex)
            {
                 return Result.Failure<OrderResponseDto>(new Error("VALIDATION_ERROR", ex.Message, ErrorType.Validation));
            }

            var response = _mapper.Map<OrderResponseDto>(order);
            return Result.Success(response);
        }

        public async Task<Result<OrderResponseDto>> UpdateItemQuantityAsync(string cashierId, int orderId, int productId, decimal quantity, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetDraftByIdAsync(orderId, cancellationToken);
            if (order == null || order.Status != OrderStatus.Draft)
            {
                return Result.Failure<OrderResponseDto>(new Error("ORDER_NOT_FOUND_OR_NOT_DRAFT", "Draft order not found.", ErrorType.NotFound));
            }

            try
            {
                order.UpdateItemQuantity(productId, quantity);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while updating item quantity in order {OrderId}", orderId);
                return Result.Failure<OrderResponseDto>(new Error("CONCURRENCY_CONFLICT", "Order was modified by another request.", ErrorType.Conflict));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                 return Result.Failure<OrderResponseDto>(new Error("VALIDATION_ERROR", ex.Message, ErrorType.Validation));
            }

            var response = _mapper.Map<OrderResponseDto>(order);
            return Result.Success(response);
        }

        public async Task<Result<DetailedOrderResponseDto>> ConfirmOrderAsync(string cashierId, int orderId, ConfirmOrderDto dto, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await _orderRepository.GetFullOrderAsync(orderId, cancellationToken);
                if (order == null || order.Status != OrderStatus.Draft)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure<DetailedOrderResponseDto>(new Error("ORDER_NOT_FOUND_OR_NOT_DRAFT", "Draft order not found.", ErrorType.NotFound));
                }

                // Optimistic Concurrency check using RowVersion
                if (!string.IsNullOrEmpty(dto.RowVersion))
                {
                    var clientRowVersion = Convert.FromBase64String(dto.RowVersion);
                    if (!clientRowVersion.SequenceEqual(order.RowVersion))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Failure<DetailedOrderResponseDto>(new Error("CONCURRENCY_CONFLICT", "Order was modified by another request.", ErrorType.Conflict));
                    }
                }

                order.Confirm(dto.PaymentMethod, dto.OrderType);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                var response = _mapper.Map<DetailedOrderResponseDto>(order);
                return Result.Success(response);
            }
            catch (DbUpdateConcurrencyException)
            {
                 await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                 _logger.LogWarning("Concurrency conflict while confirming order {OrderId}", orderId);
                 return Result.Failure<DetailedOrderResponseDto>(new Error("CONCURRENCY_CONFLICT", "Order was modified by another request.", ErrorType.Conflict));
            }
            catch (InsufficientStockException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning(ex, "Insufficient stock while confirming order {OrderId}", orderId);
                return Result.Failure<DetailedOrderResponseDto>(new Error("INSUFFICIENT_STOCK", ex.Message, ErrorType.Validation));
            }
            catch (EmptyOrderException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning(ex, "Attempted to confirm empty order {OrderId}", orderId);
                return Result.Failure<DetailedOrderResponseDto>(new Error("EMPTY_ORDER", "Order has no items.", ErrorType.Validation));
            }
            catch (Exception)
            {
                 await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                 throw;
            }
        }

        public async Task<Result> CancelDraftAsync(string cashierId, int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetDraftByIdAsync(orderId, cancellationToken);
            if (order == null || order.Status != OrderStatus.Draft)
            {
                return Result.Failure(new Error("ORDER_NOT_FOUND_OR_NOT_DRAFT", "Draft order not found.", ErrorType.NotFound));
            }

            try
            {
                order.Cancel();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
             catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while cancelling order {OrderId}", orderId);
                return Result.Failure(new Error("CONCURRENCY_CONFLICT", "Order was modified by another request.", ErrorType.Conflict));
            }

            return Result.Success();
        }
    }
}