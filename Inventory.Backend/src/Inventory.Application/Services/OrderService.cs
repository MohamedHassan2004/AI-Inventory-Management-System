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
using Inventory.Application.Interfaces.Queries;

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

        #region SUBMIT — single-transaction order creation (LEGACY)

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
        #endregion

        #region DRAFT WORKFLOW — multi-step order building before confirmation
        public async Task<Result<DetailedOrderResponseDto>> CreateDraftAsync(string cashierId, CancellationToken cancellationToken = default)
        {
            var order = Order.CreateDraft(cashierId);
            
            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = _mapper.Map<DetailedOrderResponseDto>(order);
            return Result.Success(response);
        }

        // ─────────────────────────────────────────────────────────────
        //  DRAFT MUTATIONS
        // ─────────────────────────────────────────────────────────────

        public async Task<Result<DetailedOrderResponseDto>> AddItemAsync(
            string cashierId, int orderId, AddOrderItemDto dto, CancellationToken ct = default)
        {
            var draftResult = await GetValidatedDraftAsync(cashierId, orderId, ct);
            if (!draftResult.IsSuccess)
                return Result.Failure<DetailedOrderResponseDto>(draftResult.Error);

            var product = await _productRepository.GetBySkuWithBatchesAsync(dto.SKU, ct);
            if (product == null)
                return Result.Failure<DetailedOrderResponseDto>(
                    new Error("PRODUCT_NOT_FOUND", _localizationService.GetMessage("ProductNotFound"), ErrorType.NotFound));

            return await ExecuteOnLoadedDraftAsync(draftResult.Value, orderId,
                order => order.AddItem(product, dto.Quantity), ct);
        }

        public async Task<Result<DetailedOrderResponseDto>> RemoveItemAsync(
            string cashierId, int orderId, int productId, CancellationToken ct = default)
        {
            return await ExecuteOnDraftAsync(cashierId, orderId,
                order => order.RemoveItem(productId), ct);
        }

        public async Task<Result<DetailedOrderResponseDto>> UpdateItemQuantityAsync(
            string cashierId, int orderId, int productId, decimal quantity, CancellationToken ct = default)
        {
            return await ExecuteOnDraftAsync(cashierId, orderId,
                order => order.UpdateItemQuantity(productId, quantity), ct);
        }

        public async Task<Result<DetailedOrderResponseDto>> ConfirmOrderAsync(string cashierId, int orderId, ConfirmOrderDto dto, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await _orderRepository.GetDraftForConfirmationAsync(orderId, cancellationToken);
                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure<DetailedOrderResponseDto>(DraftOrderNotFoundError());
                }

                if (!IsOwnedByCashier(order, cashierId))
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure<DetailedOrderResponseDto>(DraftOrderAccessDeniedError());
                }

                // Optimistic Concurrency check using RowVersion
                if (!string.IsNullOrEmpty(dto.RowVersion))
                {
                    var clientRowVersion = Convert.FromBase64String(dto.RowVersion);
                    if (!clientRowVersion.SequenceEqual(order.RowVersion))
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result.Failure<DetailedOrderResponseDto>(ConcurrencyConflictError());
                    }
                }

                order.ApplyDiscount(dto.DiscountPercentage ?? 0);
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
                return Result.Failure<DetailedOrderResponseDto>(ConcurrencyConflictError());
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
                return Result.Failure<DetailedOrderResponseDto>(new Error("EMPTY_ORDER", _localizationService.GetMessage("OrderIsEmpty"), ErrorType.Validation));
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Result> CancelDraftAsync(string cashierId, int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetDraftForMutationAsync(orderId, cancellationToken);
            if (order == null)
            {
                return Result.Failure(DraftOrderNotFoundError());
            }

            if (!IsOwnedByCashier(order, cashierId))
            {
                return Result.Failure(DraftOrderAccessDeniedError());
            }

            try
            {
                order.Cancel();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while cancelling order {OrderId}", orderId);
                return Result.Failure(ConcurrencyConflictError());
            }

            return Result.Success();
        }

        // ─────────────────────────────────────────────────────────────
        //  DELIVERY WORKFLOW
        // ─────────────────────────────────────────────────────────────

        public async Task<Result<DetailedOrderResponseDto>> MarkAsDeliveredAsync(string cashierId, int orderId, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetOutForDeliveryForStatusChangeAsync(orderId, cancellationToken);
            if (order == null)
            {
                return Result.Failure<DetailedOrderResponseDto>(new Error(
                    "ORDER_NOT_OUT_FOR_DELIVERY",
                    _localizationService.GetMessage("OrderNotOutForDelivery"),
                    ErrorType.NotFound));
            }

            try
            {
                order.MarkAsDelivered();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while marking order {OrderId} as delivered", orderId);
                return Result.Failure<DetailedOrderResponseDto>(ConcurrencyConflictError());
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<DetailedOrderResponseDto>(new Error("INVALID_ORDER_OPERATION", ex.Message, ErrorType.Validation));
            }

            _logger.LogInformation("Order {OrderId} marked as delivered by cashier {CashierId}", orderId, cashierId);
            var updated = await _orderRepository.GetForDetailedResponseAsync(orderId, cancellationToken);
            var response = _mapper.Map<DetailedOrderResponseDto>(updated!);
            return Result.Success(response);
        }

        public async Task<Result<DetailedOrderResponseDto>> FailDeliveryAsync(string cashierId, int orderId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var order = await _orderRepository.GetOutForDeliveryForRestockAsync(orderId, cancellationToken);
                if (order == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result.Failure<DetailedOrderResponseDto>(new Error(
                        "ORDER_NOT_OUT_FOR_DELIVERY",
                        _localizationService.GetMessage("OrderNotOutForDelivery"),
                        ErrorType.NotFound));
                }

                order.FailDelivery();

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Concurrency conflict while failing delivery for order {OrderId}", orderId);
                return Result.Failure<DetailedOrderResponseDto>(ConcurrencyConflictError());
            }
            catch (InvalidOperationException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Failure<DetailedOrderResponseDto>(new Error("INVALID_ORDER_OPERATION", ex.Message, ErrorType.Validation));
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            _logger.LogInformation("Delivery failed for order {OrderId} — stock restored. Cashier: {CashierId}", orderId, cashierId);

            var updated = await _orderRepository.GetForDetailedResponseAsync(orderId, cancellationToken);
            var response = _mapper.Map<DetailedOrderResponseDto>(updated!);
            return Result.Success(response);
        }


        // ─────────────────────────────────────────────────────────────
        //  SHARED HELPERS 
        // ─────────────────────────────────────────────────────────────

        private async Task<Result<Order>> GetValidatedDraftAsync(
            string cashierId, int orderId, CancellationToken ct)
        {
            var order = await _orderRepository.GetDraftForMutationAsync(orderId, ct);

            if (order == null || order.Status != OrderStatus.Draft)
                return Result.Failure<Order>(DraftOrderNotFoundError());

            if (!IsOwnedByCashier(order, cashierId))
                return Result.Failure<Order>(DraftOrderAccessDeniedError());

            return Result.Success(order);
        }

        private async Task<Result<DetailedOrderResponseDto>> ExecuteOnDraftAsync(
            string cashierId, int orderId, Action<Order> action, CancellationToken ct)
        {
            var draftResult = await GetValidatedDraftAsync(cashierId, orderId, ct);
            if (!draftResult.IsSuccess)
                return Result.Failure<DetailedOrderResponseDto>(draftResult.Error);

            return await ExecuteOnLoadedDraftAsync(draftResult.Value, orderId, action, ct);
        }

        private async Task<Result<DetailedOrderResponseDto>> ExecuteOnLoadedDraftAsync(
            Order order, int orderId, Action<Order> action, CancellationToken ct)
        {
            try
            {
                action(order);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict on order {OrderId}", orderId);
                return Result.Failure<DetailedOrderResponseDto>(ConcurrencyConflictError());
            }
            catch (Exception ex) when (ex is ArgumentException
                                        or InvalidOperationException
                                        or InsufficientStockException
                                        or InvalidDiscountException)
            {
                return Result.Failure<DetailedOrderResponseDto>(
                    new Error("VALIDATION_ERROR", ex.Message, ErrorType.Validation));
            }

            return Result.Success(_mapper.Map<DetailedOrderResponseDto>(order));
        }

        private static bool IsOwnedByCashier(Order order, string cashierId) =>
            order.CashierId == cashierId;

        private Error DraftOrderNotFoundError() =>
            new("ORDER_NOT_FOUND_OR_NOT_DRAFT",
                _localizationService.GetMessage("DraftOrderNotFound"),
                ErrorType.NotFound);

        private Error DraftOrderAccessDeniedError() =>
            new("DRAFT_ORDER_ACCESS_DENIED",
                _localizationService.GetMessage("DraftOrderAccessDenied"),
                ErrorType.Forbidden);

        private Error ConcurrencyConflictError() =>
            new("CONCURRENCY_CONFLICT",
                _localizationService.GetMessage("OrderModifiedByAnotherRequest"),
                ErrorType.Conflict);
    }
    #endregion
}
