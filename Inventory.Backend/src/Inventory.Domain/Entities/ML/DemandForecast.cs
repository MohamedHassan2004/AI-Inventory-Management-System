using System;

namespace Inventory.Domain.Entities.ML
{
    public class DemandForecast
    {
        public int Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public DateTime ForecastDate { get; set; }
        public double ForecastValue { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
