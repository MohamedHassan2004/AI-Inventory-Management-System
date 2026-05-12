using Inventory.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Infrastructure.Data.Seeders;

public class CategorySeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Categories.Any()) return;

        var categories = new List<Category>
        {
            new Category("Beverages", "beverages.png"),
            new Category("Dairy", "dairy.png"),
            new Category("Meat", "meat.png"),
            new Category("Bakery", "bakery.png"),
            new Category("Snacks", "snacks.png"),
            new Category("Fruits", "fruits.png"),
            new Category("Vegetables", "vegetables.png")
        };

        context.Categories.AddRange(categories);
        context.SaveChanges();
    }
}
