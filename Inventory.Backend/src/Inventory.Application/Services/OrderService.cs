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

        // ─────────────────────────────────────────────────────────────
        //  SUBMIT — single-transaction order creation
        // ─────────────────────────────────────────────────────────────

        public async Task<Result<OrderResponseDto>> SubmitAsync(
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
                return Result.Failure<OrderResponseDto>(new Error(
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
                return Result.Failure<OrderResponseDto>(new Error(
                    "INSUFFICIENT_STOCK",
                    _localizationService.GetMessage("InsufficientStock"),
                    ErrorType.Validation));
            }
            catch (InvalidDiscountException ex)
            {
                _logger.LogWarning(ex, "Invalid discount {Discount} for order", dto.DiscountPercentage);
                return Result.Failure<OrderResponseDto>(new Error(
                    "INVALID_DISCOUNT",
                    _localizationService.GetMessage("InvalidDiscount"),
                    ErrorType.Validation));
            }
            catch (EmptyOrderException ex)
            {
                _logger.LogWarning(ex, "Attempted to submit empty order");
                return Result.Failure<OrderResponseDto>(new Error(
                    "EMPTY_ORDER",
                    _localizationService.GetMessage("EmptyOrder"),
                    ErrorType.Validation));
            }

            // 5. Persist — single SaveChanges saves Order, Items, Consumptions, and batch updates
            await _orderRepository.AddAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} submitted successfully for user {UserId}", order.Id, userId);

            var response = _mapper.Map<OrderResponseDto>(order);
            return Result.Success(response);
        }
    }
}