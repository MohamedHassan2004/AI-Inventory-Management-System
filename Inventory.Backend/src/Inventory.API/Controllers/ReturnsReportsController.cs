using Inventory.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/reports/returns")]
public class ReturnsReportsController : ControllerBase
{
    private readonly IReturnsReportService _reportService;

    public ReturnsReportsController(IReturnsReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("top-products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopReturnedProducts(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    [FromQuery] int top = 5,
    CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetTopReturnedProductsAsync(
            startDate,
            endDate,
            top,
            cancellationToken);

        return Ok(result);
    }   
}