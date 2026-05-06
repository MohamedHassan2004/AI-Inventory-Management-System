using Inventory.Application.DTOs.ReturnOrder;
using Inventory.Domain.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces
{
    public interface IReturnOrderService
    {
        Task<Result<ReturnOrderResponseDto>> CreateAsync(string cashierId, CreateReturnOrderDto dto, CancellationToken cancellationToken = default);
    }
}
