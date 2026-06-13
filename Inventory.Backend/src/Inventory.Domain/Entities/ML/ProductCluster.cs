using System;

namespace Inventory.Domain.Entities.ML
{
    public class ProductCluster
    {
        public int Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ClusterName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
