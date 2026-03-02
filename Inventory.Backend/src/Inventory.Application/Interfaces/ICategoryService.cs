using Inventory.Application.DTOs.Category;
using Inventory.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
namespace Inventory.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<Result<IEnumerable<CategoryResponseDto>>> GetAllAsync();
        Task<Result> UpdateCategoryImageAsync(int id, UpdateCategoryImageDto dto);
        Task<Result<CategoryResponseDto>> UpdateAsync(int id, UpdateCategoryDto dto);
        Task<Result<CategoryResponseDto>> CreateAsync(CreateCategoryDto dto);
    }
}
