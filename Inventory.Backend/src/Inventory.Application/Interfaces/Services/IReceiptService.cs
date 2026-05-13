using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces.Services;

public interface IReceiptService
{
    Task<byte[]> GenerateReceiptAsync(int orderId, CancellationToken cancellationToken = default);
}
