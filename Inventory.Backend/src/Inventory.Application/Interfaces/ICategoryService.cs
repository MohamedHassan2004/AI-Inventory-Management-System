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
        Task<Result<IEnumerable<CategoryResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result> UpdateCategoryImageAsync(int id, UpdateCategoryImageDto dto, CancellationToken cancellationToken = default);
        Task<Result<CategoryResponseDto>> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken = default);
        Task<Result<CategoryResponseDto>> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default);

        
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
