using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports.Returns;

namespace Inventory.Application.Interfaces.Queries.Reports;

public interface IReturnsReportQuery
{
    Task<IEnumerable<TopReturnedProductDto>> GetTopReturnedProductsAsync(
    DateTime startDate,
    DateTime endDate,
    int top,
    CancellationToken cancellationToken);
}