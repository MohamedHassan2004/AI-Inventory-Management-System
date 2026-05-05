using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    public class ReturnOrderItemConfiguration : IEntityTypeConfiguration<ReturnOrderItem>
    {
        public void Configure(EntityTypeBuilder<ReturnOrderItem> builder)
        {

            builder.HasKey(i => i.Id);

            builder.Property(i => i.Quantity)
                   .HasPrecision(18, 2);

            builder.Property(i => i.UnitPrice)
                   .HasPrecision(18, 2);

            // Item → Original OrderItem
            builder.HasOne(i => i.OriginalOrderItem)
                   .WithMany()
                   .HasForeignKey(i => i.OriginalOrderItemId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Item → Product
            builder.HasOne(i => i.Product)
                   .WithMany()
                   .HasForeignKey(i => i.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}