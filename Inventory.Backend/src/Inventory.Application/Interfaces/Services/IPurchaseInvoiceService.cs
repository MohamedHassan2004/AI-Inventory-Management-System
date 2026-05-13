using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces.Services;

public interface IPurchaseInvoiceService
{
    Task<byte[]> GenerateInvoiceAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
}
