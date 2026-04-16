using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);

            builder.Property(o => o.CashierId).IsRequired();

            builder.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(o => o.Type)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(o => o.PaymentMethod)
                .HasConversion<string>();

            builder.Property(o => o.SubTotal).HasPrecision(18, 2);
            builder.Property(o => o.DiscountPercentage).HasPrecision(5, 2);
            builder.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            builder.Property(o => o.TaxAmount).HasPrecision(18, 2);
            builder.Property(o => o.FinalTotal).HasPrecision(18, 2);

            builder.HasOne(o => o.Cashier)
                .WithMany()
                .HasForeignKey(o => o.CashierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}