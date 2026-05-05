using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations
{
    public class StockConsumptionConfiguration : IEntityTypeConfiguration<StockConsumption>
    {
        public void Configure(EntityTypeBuilder<StockConsumption> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Quantity)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(x => x.ReturnedQuantity)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.StockBatch)
                .WithMany()
                .HasForeignKey(x => x.StockBatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.OrderItem)
                .WithMany(x => x.StockConsumptions)
                .HasForeignKey(x => x.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.OrderItemId);
        }
    }
}
