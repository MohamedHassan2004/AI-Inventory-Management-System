using Inventory.Application.Interfaces.Queries.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reports/inventory")]
public class InventoryReportsController : ControllerBase
{
    private readonly IInventoryReportQuery _reportService;

    public InventoryReportsController(IInventoryReportQuery reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("expiring-batches")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringBatches(
    [FromQuery] int days = 30,
    CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetExpiringBatchesAsync(
            days,
            cancellationToken);

        return Ok(result);
    }
    [HttpGet("dead-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeadStockProducts(
    [FromQuery] int days = 90,
    CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDeadStockProductsAsync(
            days,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockProducts(
    CancellationToken cancellationToken)
    {
        var result = await _reportService.GetLowStockProductsAsync(
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("out-of-stock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutOfStockProducts(
    CancellationToken cancellationToken)
    {
        var result = await _reportService.GetOutOfStockProductsAsync(
            cancellationToken);

        return Ok(result);
    }
    [HttpGet("turnover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryTurnover(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetInventoryTurnoverAsync(
            startDate,
            endDate,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }
}