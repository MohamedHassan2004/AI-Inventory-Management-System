using Inventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reports/inventory")]
public class InventoryReportsController : ControllerBase
{
    private readonly IInventoryReportService _reportService;

    public InventoryReportsController(IInventoryReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("stock-value")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockValue(
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockValueAsync(
            cancellationToken);

        return Ok(result);
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

    [HttpGet("advanced-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdvancedSummary(
    [FromQuery] int expiryDays = 30,
    [FromQuery] int deadStockDays = 90,
    CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetAdvancedSummaryAsync(
            expiryDays,
            deadStockDays,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("stock-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockSummary(
    CancellationToken cancellationToken)
    {
        var result = await _reportService.GetStockSummaryAsync(
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
    [HttpGet("turnover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryTurnover(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    CancellationToken cancellationToken)
    {
        var result = await _reportService.GetInventoryTurnoverAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }
}