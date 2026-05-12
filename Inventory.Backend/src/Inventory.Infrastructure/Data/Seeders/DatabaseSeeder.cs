using Inventory.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Data.Seeders;

public class DatabaseSeeder
{
    public static async Task SeedAllAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        // Ensure roles and admin use are seeded first from ApplicationDbContextSeed
        // (Assuming ApplicationDbContextSeed handles roles & admin)

        // Seed users
        await UserSeeder.SeedAsync(userManager);

        // Seed non-dependent entities
        CategorySeeder.Seed(context);
        SupplierSeeder.Seed(context);

        // Seed dependent entities
        ProductSeeder.Seed(context); // Depends on categories & suppliers
        PurchaseOrderSeeder.Seed(context); // Depends on products & suppliers
        OrderSeeder.Seed(context);   // Depends on products (with batches) & users
        ReturnOrderSeeder.Seed(context); // Depends on completed orders
    }
}