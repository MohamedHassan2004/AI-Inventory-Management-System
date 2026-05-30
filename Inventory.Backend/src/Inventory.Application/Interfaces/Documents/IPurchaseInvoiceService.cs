using Inventory.Domain.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Application.Interfaces.Documents;

public interface IPurchaseInvoiceService
{
    Task<Result<byte[]>> GenerateInvoiceAsync(int purchaseOrderId, CancellationToken cancellationToken = default);
}
