using Inventory.Application.DTOs.Category;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    public class CategoriesController : ApiBaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);
            return HandleResult(result);
        }

        // PUT: api/categories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var result = await _categoryService.UpdateAsync(id, dto);
            return HandleResult(result);
        }

        // PATCH: api/categories/updateCategoryImg/{id}
        [HttpPatch("updateCategoryImg/{id}")]
        public async Task<IActionResult> UpdateImage(int id, [FromForm] UpdateCategoryImageDto dto)
        {
            var result = await _categoryService.UpdateCategoryImageAsync(id, dto);
            return HandleResult(result);
        }

        // GET: api/categories
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _categoryService.GetAllAsync();
            return HandleResult(result);
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _categoryService.DeleteAsync(id);
            return HandleResult(result);
        }
    }
}