using Inventory.Application.DTOs.Supplier;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Queries.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    public class SuppliersController : ActiveApiBaseController
    {
        private readonly ISupplierService _supplierService;
        private readonly ISupplierReportQuery _supplierReportService;

        public SuppliersController(ISupplierService supplierService, ISupplierReportQuery supplierReportService)
        {
            _supplierService = supplierService;
            _supplierReportService = supplierReportService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSuppliers(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var result = await _supplierReportService.GetSuppliersReportAsync(
                startDate,
                endDate,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("deleted")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeleted(CancellationToken cancellationToken)
        {
            var result = await _supplierService.GetDeletedSuppliersAsync(cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _supplierService.GetSupplierByIdAsync(id, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto, CancellationToken cancellationToken)
        {
            var result = await _supplierService.CreateSupplierAsync(dto, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplierDto dto, CancellationToken cancellationToken)
        {
            var result = await _supplierService.UpdateSupplierAsync(id, dto, cancellationToken);
            return HandleResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _supplierService.DeleteSupplierAsync(id, cancellationToken);
            return HandleResult(result);
        }

        [HttpPut("{id}/restore")]
        public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
        {
            var result = await _supplierService.RestoreSupplierAsync(id, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("{id}/notes")]
        public async Task<IActionResult> GetNotes(int id, CancellationToken cancellationToken)
        {
            var result = await _supplierService.GetSupplierNotesAsync(id, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{id}/ratings")]
        public async Task<IActionResult> AddRating(int id, [FromBody] AddSupplierRatingDto dto, CancellationToken cancellationToken)
        {
            var result = await _supplierService.AddSupplierRatingAsync(id, dto, cancellationToken);
            return HandleResult(result);
        }
    }
}