using Inventory.Application.DTOs.StockBatch;
using Inventory.Domain.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces
{
    public interface IStockBatchService
    {
        Task<Result<StockBatchResponseDto>> CreateAsync(CreateStockBatchDto dto, CancellationToken cancellationToken = default);
        Task<Result<StockBatchResponseDto>> UpdateAsync(int id, UpdateStockBatchDto dto, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<StockBatchResponseDto>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<StockBatchResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<StockBatchResponseDto>>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<StockBatchResponseDto>>> GetExpiringBatchesAsync(int daysUntilExpiry, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
