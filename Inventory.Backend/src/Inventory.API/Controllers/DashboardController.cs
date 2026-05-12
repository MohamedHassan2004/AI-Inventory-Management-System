using Inventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryAsync(
            cancellationToken);

        return Ok(result);
    }
}