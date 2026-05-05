using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Data.Configurations
{
    public class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
    {
        public void Configure(EntityTypeBuilder<StockBatch> builder)
        {
            builder.HasKey(sb => sb.Id);

            builder.Property(sb => sb.RowVersion).IsRowVersion();

            builder.Property(sb => sb.UnitCost)
                .HasColumnType("decimal(18,2)");

            builder.Property(sb => sb.OriginalQuantity)
                .HasColumnType("decimal(18,2)");

            builder.Property(sb => sb.RemainingQuantity)
                .HasColumnType("decimal(18,2)");

            builder.HasOne(sb => sb.Product)
                .WithMany(p => p.Batches)
                .HasForeignKey(sb => sb.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sb => sb.Supplier)
                .WithMany()
                .HasForeignKey(sb => sb.SupplierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
