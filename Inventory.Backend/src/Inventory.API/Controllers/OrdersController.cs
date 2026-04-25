using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
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

        public OrdersController(
            IOrderService orderService,
            IOrderQueryService orderQueryService)
        {
            _orderService = orderService;
            _orderQueryService = orderQueryService;
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
        //  QUERIES
        // ─────────────────────────────────────────────────────────────

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _orderQueryService.GetByIdAsync(id, cancellationToken);
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
            var result = await _orderQueryService.GetItemsByOrderIdAsync(orderId, cancellationToken);
            return HandleResult(result);
        }
    }
}