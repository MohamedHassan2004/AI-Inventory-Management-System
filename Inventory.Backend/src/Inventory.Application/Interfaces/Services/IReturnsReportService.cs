using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Application.DTOs.Reports.Returns;

namespace Inventory.Application.Interfaces.Services;

public interface IReturnsReportService
{
    Task<IEnumerable<TopReturnedProductDto>> GetTopReturnedProductsAsync(
    DateTime startDate,
    DateTime endDate,
    int top,
    CancellationToken cancellationToken);
}