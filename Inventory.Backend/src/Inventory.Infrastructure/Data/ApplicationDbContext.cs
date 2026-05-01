using Inventory.Domain.Entities;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierNotes> SupplierNotes { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<StockBatch> StockBatches { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.Entity<Supplier>().HasQueryFilter(supplier => !supplier.IsDeleted);

        modelBuilder.Entity<SupplierNotes>()
               .HasOne(x => x.Supplier)
               .WithMany(x => x.SupplierNotes)
               .IsRequired(false);



    }
}
