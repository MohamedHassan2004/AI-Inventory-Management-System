using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MLController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MLController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("recommendations/{sku}")]
        public async Task<IActionResult> GetRecommendations(string sku, [FromQuery] int topN = 5)
        {
            var recs = await _context.ProductRecommendations
                .Where(r => r.SKU == sku)
                .OrderByDescending(r => r.Score)
                .Take(topN)
                .Select(r => r.RecommendedSKU)
                .ToListAsync();

            return Ok(new { sku, recommendations = recs });
        }

        [HttpGet("clusters")]
        public async Task<IActionResult> GetClusters()
        {
            var clusters = await _context.ProductClusters
                .GroupBy(c => c.ClusterId)
                .Select(g => new
                {
                    cluster_id = g.Key,
                    items = g.Select(c => new { sku = c.SKU }).ToList()
                })
                .OrderBy(c => c.cluster_id)
                .ToListAsync();

            return Ok(new { clusters });
        }

        [HttpGet("forecast/{sku}")]
        public async Task<IActionResult> GetForecast(string sku)
        {
            var forecasts = await _context.DemandForecasts
                .Where(f => f.SKU == sku)
                .OrderBy(f => f.ForecastDate)
                .ToListAsync();

            if (!forecasts.Any())
            {
                return NotFound(new { message = $"Forecast not available for SKU '{sku}'." });
            }

            return Ok(new
            {
                sku = sku,
                forecast_dates = forecasts.Select(f => f.ForecastDate.ToString("yyyy-MM-dd")),
                forecast_values = forecasts.Select(f => f.ForecastValue),
                lower_bounds = forecasts.Select(f => f.LowerBound),
                upper_bounds = forecasts.Select(f => f.UpperBound)
            });
        }
    }
}
