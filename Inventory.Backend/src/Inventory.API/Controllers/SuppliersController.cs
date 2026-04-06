using Inventory.Application.DTOs.Supplier;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    public class SuppliersController : ActiveApiBaseController
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _supplierService.GetAllSuppliersAsync(cancellationToken);
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