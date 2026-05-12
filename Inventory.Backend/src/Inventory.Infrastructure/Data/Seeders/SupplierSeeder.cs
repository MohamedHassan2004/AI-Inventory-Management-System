using Inventory.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Infrastructure.Data.Seeders;

public class SupplierSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Suppliers.Any()) return;

        var suppliers = new List<Supplier>();
        for (int i = 1; i <= 10; i++)
        {
            suppliers.Add(new Supplier(
                name: $"Supplier {i}",
                phoneNumber: $"555-01{i:D2}",
                contactInfo: $"Contact Info for {i}",
                address: $"Address {i}"
            ));
        }

        context.Suppliers.AddRange(suppliers);
        context.SaveChanges();
    }
}