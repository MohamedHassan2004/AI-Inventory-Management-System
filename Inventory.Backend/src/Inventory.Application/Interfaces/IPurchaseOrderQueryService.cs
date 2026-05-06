using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface IPurchaseOrderQueryService
    {
        Task<Result<PurchaseOrderResponseDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<Result<PagedResult<PurchaseOrderResponseDto>>> GetAllAsync(PurchaseOrderFilter filter, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<PurchaseOrderItemResponseDto>>> GetItemsByPurchaseOrderIdAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
    }
}
