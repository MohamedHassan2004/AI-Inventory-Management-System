using Inventory.Application.DTOs.Order;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [Route("api/orders")]
    [Authorize(Roles = "Cashier,Admin")]
    public class OrderItemsController : ActiveApiBaseController
    {
        private readonly IOrderService _orderService;
        private readonly IOrderQueryService _orderQueryService;

        public OrderItemsController(
            IOrderService orderService,
            IOrderQueryService orderQueryService)
        {
            _orderService = orderService;
            _orderQueryService = orderQueryService;
        }

        // POST: api/orders/{orderId}/items
        [HttpPost("{orderId}/items")]
        public async Task<IActionResult> AddItem(
            int orderId,
            [FromBody] OrderItemDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _orderService.AddItemAsync(orderId, dto, cancellationToken);
            return HandleResult(result);
        }

        // PATCH: api/orders/{orderId}/items/{itemId}
        [HttpPatch("{orderId}/items/{itemId}")]
        public async Task<IActionResult> UpdateQuantity(
            int orderId,
            int itemId,
            [FromBody] UpdateOrderItemQuantityDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _orderService.UpdateItemQuantityAsync(
                orderId,
                itemId,
                dto.Quantity,
                cancellationToken);

            return HandleResult(result);
        }

        // DELETE: api/orders/{orderId}/items/{itemId}
        [HttpDelete("{orderId}/items/{itemId}")]
        public async Task<IActionResult> RemoveItem(
            int orderId,
            int itemId,
            CancellationToken cancellationToken)
        {
            var result = await _orderService.RemoveItemAsync(orderId, itemId, cancellationToken);
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

        // GET: api/orders/items/product/{productId}
        [HttpGet("items/product/{productId}")]
        public async Task<IActionResult> GetOrdersByProduct(
            int productId,
            CancellationToken cancellationToken)
        {
            var result = await _orderQueryService.GetAllAsync(new OrderFilter { ProductId = productId }, cancellationToken);
            return HandleResult(result);
        }
    }
}