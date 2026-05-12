using Inventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reports/users")]
public class UsersReportsController : ControllerBase
{
    private readonly IUsersReportService _reportService;

    public UsersReportsController(IUsersReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("cashier-sales")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCashierSales(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetCashierSalesAsync(
            startDate,
            endDate,
            cancellationToken);

        return Ok(result);
    }
    [HttpGet("status-breakdown")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusBreakdown(
    CancellationToken cancellationToken)
    {
        var result = await _reportService.GetUserStatusBreakdownAsync(
            cancellationToken);

        return Ok(result);
    }
}