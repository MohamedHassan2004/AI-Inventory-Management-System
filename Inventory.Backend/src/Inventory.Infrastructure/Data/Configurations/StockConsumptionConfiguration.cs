using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations
{
    public class StockConsumptionConfiguration : IEntityTypeConfiguration<StockConsumption>
    {
        public void Configure(EntityTypeBuilder<StockConsumption> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Quantity)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.HasOne(c => c.Batch)
                .WithMany()
                .HasForeignKey(c => c.StockBatchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}