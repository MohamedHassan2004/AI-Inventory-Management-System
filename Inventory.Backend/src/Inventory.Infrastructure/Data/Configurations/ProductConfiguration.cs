using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.SKU)
                .IsRequired()
                .HasMaxLength(50);
            
            builder.HasIndex(p => p.SKU)
                .IsUnique();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.SellingPrice)
                .HasColumnType("decimal(18,2)");

            builder.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            var batchesNavigation = builder.Metadata.FindNavigation(nameof(Product.Batches));
            batchesNavigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(p => p.Batches)
                .WithOne()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
