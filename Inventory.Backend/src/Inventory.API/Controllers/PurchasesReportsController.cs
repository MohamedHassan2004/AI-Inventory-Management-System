using Inventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reports/purchases")]
public class PurchasesReportsController : ControllerBase
{
    private readonly IPurchasesReportService _reportService;

    public PurchasesReportsController(IPurchasesReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPurchasesSummaryAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }
    [HttpGet("top-suppliers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopSuppliers(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] int top = 5,
    CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetTopSuppliersAsync(
            startDate,
            endDate,
            top,
            cancellationToken);

        return Ok(result);
    }
    [HttpGet("supplier-performance")]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> GetSupplierPerformance(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    CancellationToken cancellationToken)
{
    var result = await _reportService.GetSupplierPerformanceAsync(
        startDate,
        endDate,
        cancellationToken);

    return Ok(result);
}
}   