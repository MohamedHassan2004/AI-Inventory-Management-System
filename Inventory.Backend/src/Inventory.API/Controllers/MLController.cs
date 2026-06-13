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
            var recs = await (
                from r in _context.ProductRecommendations
                join p in _context.Products
                    on r.RecommendedSKU equals p.SKU
                where r.SKU == sku
                orderby r.Score descending
                select new
                {
                    id = p.Id,
                    sku = p.SKU,
                    name = p.Name,
                    score = r.Score
                }
            )
            .Take(topN)
            .ToListAsync();

            return Ok(new { sku, recommendations = recs });
        }

        [HttpGet("clusters")]
        public async Task<IActionResult> GetClusters()
        {
            var clusters = await _context.ProductClusters
                .GroupBy(c => c.ClusterName)
                .Select(g => new
                {
                    cluster_name = g.Key,
                    items = (
    from c in g
    join p in _context.Products
        on c.SKU equals p.SKU
    select new
    {
        id = p.Id,
        sku = p.SKU,
        name = p.Name
    }
).ToList()
                })
                .OrderBy(c => c.cluster_name)
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
