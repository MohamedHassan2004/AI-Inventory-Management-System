using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.DTOs.PurchaseOrder;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces
{
    public interface IPurchaseOrderService
    {
        Task<Result<PurchaseOrderResponseDto>> SubmitAsync(SubmitPurchaseOrderDto dto, CancellationToken cancellationToken = default);
    }
}
