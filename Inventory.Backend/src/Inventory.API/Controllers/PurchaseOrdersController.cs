using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin, InventoryStaff")]
    public class PurchaseOrdersController : ActiveApiBaseController
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IPurchaseOrderQueryService _purchaseOrderQueryService;

        public PurchaseOrdersController(
            IPurchaseOrderService purchaseOrderService,
            IPurchaseOrderQueryService purchaseOrderQueryService)
        {
            _purchaseOrderService = purchaseOrderService;
            _purchaseOrderQueryService = purchaseOrderQueryService;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> Submit(
            [FromBody] SubmitPurchaseOrderDto dto,
            CancellationToken cancellationToken)
        {
            var result = await _purchaseOrderService.SubmitAsync(dto, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _purchaseOrderQueryService.GetByIdAsync(id, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] PurchaseOrderFilter filter,
            CancellationToken cancellationToken)
        {
            var result = await _purchaseOrderQueryService.GetAllAsync(filter, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id}/items")]
        public async Task<IActionResult> GetItemsByPurchaseOrder(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _purchaseOrderQueryService.GetItemsByPurchaseOrderIdAsync(id, cancellationToken);
            return HandleResult(result);
        }
    }
}
