using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

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

        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateOrderDto dto,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _orderService.CreateAsync(dto, userId, cancellationToken);
            return HandleResult(result);
        }

        // PATCH: api/orders/{id}/discount/{discountPercentage}
        [HttpPatch("{id}/discount/{discountPercentage}")]
        public async Task<IActionResult> ApplyDiscount(
            int id,
            decimal discountPercentage,
            CancellationToken cancellationToken)
        {
            var result = await _orderService.ApplyDiscountAsync(id, discountPercentage, cancellationToken);
            return HandleResult(result);
        }

        // PATCH: api/orders/{id}/complete
        [HttpPatch("{id}/complete")]
        public async Task<IActionResult> Complete(
            int id,
            [FromBody] CompleteOrderDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _orderService.CompleteAsync(id, dto, cancellationToken);
            return HandleResult(result);
        }

        // PATCH: api/orders/{id}/cancel
        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _orderService.CancelAsync(id, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _orderQueryService.GetByIdAsync(id, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/orders/pending
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending(
            CancellationToken cancellationToken)
        {
            var result = await _orderQueryService.GetPendingAsync(cancellationToken);
            return HandleResult(result);
        }
    }
}