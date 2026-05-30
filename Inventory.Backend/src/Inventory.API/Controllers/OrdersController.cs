using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Documents;
using Inventory.Application.Interfaces.Queries;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Cashier,Admin")]
    public class OrdersController : ActiveApiBaseController
    {
        private readonly IOrderService _orderService;
        private readonly IOrderQueryService _orderQueryService;
        private readonly IReceiptService _receiptService;

        public OrdersController(
            IOrderService orderService,
            IOrderQueryService orderQueryService,
            IReceiptService receiptService)
        {
            _orderService = orderService;
            _orderQueryService = orderQueryService;
            _receiptService = receiptService;
        }

        // ─────────────────────────────────────────────────────────────
        //  SUBMIT  — the primary cashier action
        // ─────────────────────────────────────────────────────────────

        // POST: api/orders/submit
        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            [FromBody] SubmitOrderDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.SubmitAsync(userId, dto, cancellationToken);
            return HandleResult(result);
        }

        // ─────────────────────────────────────────────────────────────
        //  DRAFT WORKFLOW
        // ─────────────────────────────────────────────────────────────

        // POST: api/orders/draft
        [HttpPost("draft")]
        public async Task<IActionResult> CreateDraft(
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.CreateDraftAsync(userId, cancellationToken);
            return HandleResult(result);
        }

        // POST: api/orders/{id}/items
        [HttpPost("{id}/items")]
        public async Task<IActionResult> AddItem(
            int id,
            [FromBody] AddOrderItemDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.AddItemAsync(userId, id, dto, cancellationToken);
            return HandleResult(result);
        }

        // DELETE: api/orders/{id}/items/{productId}
        [HttpDelete("{id}/items/{productId}")]
        public async Task<IActionResult> RemoveItem(
            int id,
            int productId,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.RemoveItemAsync(userId, id, productId, cancellationToken);
            return HandleResult(result);
        }

        // PUT: api/orders/{id}/items/{productId}
        [HttpPut("{id}/items/{productId}")]
        public async Task<IActionResult> UpdateItemQuantity(
            int id,
            int productId,
            [FromBody] UpdateOrderItemDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.UpdateItemQuantityAsync(userId, id, productId, dto.Quantity, cancellationToken);
            return HandleResult(result);
        }

        // POST: api/orders/{id}/confirm
        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(
            int id,
            [FromBody] ConfirmOrderDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.ConfirmOrderAsync(userId, id, dto, cancellationToken);
            return HandleResult(result);
        }

        // DELETE: api/orders/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelDraft(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.CancelDraftAsync(userId, id, cancellationToken);
            return HandleResult(result);
        }

        // POST: api/orders/{id}/deliver
        [HttpPost("{id}/deliver")]
        public async Task<IActionResult> MarkAsDelivered(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.MarkAsDeliveredAsync(userId, id, cancellationToken);
            return HandleResult(result);
        }

        // POST: api/orders/{id}/fail-delivery
        [HttpPost("{id}/fail-delivery")]
        public async Task<IActionResult> FailDelivery(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderService.FailDeliveryAsync(userId, id, cancellationToken);
            return HandleResult(result);
        }

        // ─────────────────────────────────────────────────────────────
        //  QUERIES
        // ─────────────────────────────────────────────────────────────

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderQueryService.GetByIdAsync(userId, id, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/orders/out-for-delivery?sortBy=OrderDate&page=1&pageSize=20
        [HttpGet("out-for-delivery")]
        public async Task<IActionResult> GetOutForDelivery(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] OrderSortBy sortBy = OrderSortBy.OrderDate,
            [FromQuery] bool sortDescending = true,
            CancellationToken cancellationToken = default)
        {
            var filter = new OrderFilter
            {
                Status = OrderStatus.OutForDelivery,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending
            };
            var result = await _orderQueryService.GetAllAsync(filter, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/orders?status=Completed&sortBy=FinalTotal&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] OrderFilter filter,
            CancellationToken cancellationToken)
        {
            var result = await _orderQueryService.GetAllAsync(filter, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/orders/{orderId}/items
        [HttpGet("{orderId}/items")]
        public async Task<IActionResult> GetItemsByOrder(
            int orderId,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _orderQueryService.GetItemsByOrderIdAsync(userId, orderId, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/orders/{id}/receipt
        [HttpGet("{id}/receipt")]
        public async Task<IActionResult> GetReceipt(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _receiptService.GenerateReceiptAsync(id, cancellationToken);
            if (!result.IsSuccess)
            {
                return HandleResult(result);
            }
            return File(result.Value, "application/pdf", $"receipt-{id}.pdf");
        }
    }
}
