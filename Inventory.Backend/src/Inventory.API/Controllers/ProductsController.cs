using Inventory.Application.DTOs.Product;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    public class ProductsController : ActiveApiBaseController
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // POST: api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
        {
            var result = await _productService.CreateAsync(dto, cancellationToken);
            return HandleResult(result);
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
        {
            var result = await _productService.UpdateAsync(id, dto, cancellationToken);
            return HandleResult(result);
        }

        // PATCH: api/products/{id}/updatePrice
        [HttpPatch("{id}/updatePrice")]
        public async Task<IActionResult> UpdatePrice(int id, [FromBody] UpdateProductPriceDto dto, CancellationToken cancellationToken)
        {
            var result = await _productService.UpdatePriceAsync(id, dto, cancellationToken);
            return HandleResult(result);
        }

        // PATCH: api/products/{id}/updateReorderPoint
        [HttpPatch("{id}/updateReorderPoint")]
        public async Task<IActionResult> UpdateReorderPoint(int id, [FromBody] UpdateProductReorderPointDto dto, CancellationToken cancellationToken)
        {
            var result = await _productService.UpdateReorderPointAsync(id, dto, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _productService.GetAllAsync(cancellationToken);
            return HandleResult(result);
        }

        // GET: api/products/low-stock
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStockProducts(CancellationToken cancellationToken)
        {
            var result = await _productService.GetLowStockProductsAsync(cancellationToken);
            return HandleResult(result);
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var result = await _productService.GetByIdAsync(id, cancellationToken);
            return HandleResult(result);
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var result = await _productService.DeleteAsync(id, cancellationToken);
            return HandleResult(result);
        }

        // GET: api/products/search?q=pepsi
        // Searches by product name (Full-Text Search) or exact SKU match.
        // Intended for the cashier product-picker; returns lightweight lookup results.
        [HttpGet("search")]
        [Authorize(Roles = "Cashier,Admin")]
        public async Task<IActionResult> Search(
            [FromQuery] string q,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { error = "Search term 'q' is required." });

            var result = await _productService.SearchAsync(q, cancellationToken);
            return HandleResult(result);
        }
    }
}
