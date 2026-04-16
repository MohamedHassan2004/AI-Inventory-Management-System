using Inventory.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Data.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.IdentityImgUrl)
                .HasMaxLength(500);

            builder.Property(u => u.PhoneNumber)
                 .IsRequired();

            builder.HasIndex(u => u.PhoneNumber)
                .IsUnique();

            builder.Property(u => u.RejectionReason)
                .HasMaxLength(500);

            builder.HasQueryFilter(u => !u.IsDeleted);
        }
    }
}
