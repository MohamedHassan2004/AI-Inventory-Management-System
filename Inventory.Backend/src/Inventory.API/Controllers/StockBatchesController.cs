using Inventory.API.Controllers;
using Inventory.Application.DTOs.StockBatch;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    public class StockBatchesController : ActiveApiBaseController
    {
        private readonly IStockBatchService _stockBatchService;

        public StockBatchesController(IStockBatchService stockBatchService)
        {
            _stockBatchService = stockBatchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _stockBatchService.GetAllAsync();
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _stockBatchService.GetByIdAsync(id);
            return HandleResult(result);
        }

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var result = await _stockBatchService.GetByProductIdAsync(productId);
            return HandleResult(result);
        }

        [HttpGet("supplier/{supplierId}")]
        public async Task<IActionResult> GetBySupplierId(int supplierId)
        {
            var result = await _stockBatchService.GetBySupplierIdAsync(supplierId);
            return HandleResult(result);
        }

        [HttpGet("expiring/{days}")]
        public async Task<IActionResult> GetExpiringBatches(int days)
        {
            var result = await _stockBatchService.GetExpiringBatchesAsync(days);
            return HandleResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStockBatchDto dto)
        {
            var result = await _stockBatchService.CreateAsync(dto);
            return HandleResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStockBatchDto dto)
        {
            var result = await _stockBatchService.UpdateAsync(id, dto);
            return HandleResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _stockBatchService.DeleteAsync(id);
            return HandleResult(result);
        }
    }
}
