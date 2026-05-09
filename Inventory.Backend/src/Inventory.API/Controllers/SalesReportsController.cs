using Microsoft.AspNetCore.Mvc;

using Inventory.Application.Interfaces.Services;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reports/sales")]
public class SalesReportsController : ControllerBase
{
    private readonly ISalesReportService _salesReportService;

    public SalesReportsController(ISalesReportService salesReportService)
    {
        _salesReportService = salesReportService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await _salesReportService.GetSalesSummaryAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }
    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopSellingProducts(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] int top = 5,
    CancellationToken cancellationToken = default)
    {
        var result = await _salesReportService.GetTopSellingProductsAsync(
            startDate,
            endDate,
            top,
            cancellationToken);

        return Ok(result);
    }
    [HttpGet("analytics")]
    public async Task<IActionResult> GetSalesAnalytics(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    CancellationToken cancellationToken)
    {
        var result = await _salesReportService.GetSalesAnalyticsAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }
}