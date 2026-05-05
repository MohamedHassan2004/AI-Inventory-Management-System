using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [Route("api/return-orders")]
    [Authorize(Roles = "Cashier,Admin")]
    public class ReturnOrdersController : ActiveApiBaseController
    {
        private readonly IReturnOrderService _returnOrderService;
        private readonly IReturnOrderQueryService _queryService;

        public ReturnOrdersController(
            IReturnOrderService returnOrderService,
            IReturnOrderQueryService queryService)
        {
            _returnOrderService = returnOrderService;
            _queryService = queryService;
        }

        // POST: api/return-orders
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateReturnOrderDto dto,
            CancellationToken cancellationToken)
        {
            var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _returnOrderService.CreateAsync(cashierId, dto, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/return-orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var result = await _queryService.GetByIdAsync(id, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/return-orders
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] ReturnOrderFilter filter,
            CancellationToken cancellationToken)
        {
            var result = await _queryService.GetAllAsync(filter, cancellationToken);
            return HandleResult(result);
        }
    }
}
