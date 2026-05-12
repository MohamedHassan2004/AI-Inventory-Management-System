using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations
{
    public class OrderItemBatchAllocationConfiguration : IEntityTypeConfiguration<OrderItemBatchAllocation>
    {
        public void Configure(EntityTypeBuilder<OrderItemBatchAllocation> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.QuantityTaken)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(a => a.ReturnedQuantity)
                .IsRequired()
                .HasPrecision(18, 2)
                .HasDefaultValue(0m);

            builder.Ignore(a => a.RemainingToReturn);

            builder.HasOne(a => a.OrderItem)
                .WithMany(i => i.Allocations)
                .HasForeignKey(a => a.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.StockBatch)
                .WithMany()
                .HasForeignKey(a => a.StockBatchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
