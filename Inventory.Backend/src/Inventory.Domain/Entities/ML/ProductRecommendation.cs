using System;

namespace Inventory.Domain.Entities.ML
{
    public class ProductRecommendation
    {
        public int Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string RecommendedSKU { get; set; } = string.Empty;
        public double Score { get; set; } // e.g. confidence or lift
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
