using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations
{
    public class ReturnOrderConfiguration : IEntityTypeConfiguration<ReturnOrder>
    {
        public void Configure(EntityTypeBuilder<ReturnOrder> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Reason)
                   .HasMaxLength(500);

            builder.Property(r => r.TotalRefundAmount)
                   .HasPrecision(18, 2);

            // ReturnOrder → Original Order
            builder.HasOne(r => r.OriginalOrder)
                   .WithMany()
                   .HasForeignKey(r => r.OriginalOrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ReturnOrder → Cashier
            builder.HasOne(r => r.Cashier)
                   .WithMany()
                   .HasForeignKey(r => r.CashierId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // ReturnOrder → Items
            builder.HasMany(r => r.Items)
                   .WithOne()
                   .HasForeignKey(i => i.ReturnOrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}