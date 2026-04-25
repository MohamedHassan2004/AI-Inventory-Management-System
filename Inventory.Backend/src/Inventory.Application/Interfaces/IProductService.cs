using Inventory.Application.DTOs.Product;
using Inventory.Domain.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces
{
    public interface IProductService
    {
        Task<Result<ProductResponseDto>> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
        Task<Result<ProductResponseDto>> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default);
        Task<Result> UpdatePriceAsync(int id, UpdateProductPriceDto dto, CancellationToken cancellationToken = default);
        Task<Result> UpdateReorderPointAsync(int id, UpdateProductReorderPointDto dto, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<ProductResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<ProductResponseDto>>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);
        Task<Result<ProductResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Full-text search by name (FREETEXT) or exact SKU — for the cashier product picker.
        /// </summary>
        Task<Result<IEnumerable<ProductLookupDto>>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    }
}
