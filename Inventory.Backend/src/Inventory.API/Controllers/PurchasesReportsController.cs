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

    [HttpGet("suppliers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliers(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetSuppliersReportAsync(
            startDate,
            endDate,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }
}   