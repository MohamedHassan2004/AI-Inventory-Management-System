using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Inventory.Infrastructure.Data.Seeders;

public class PurchaseOrderSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Set<PurchaseOrder>().Any()) return;

        var suppliers = context.Suppliers.ToList();
        var products = context.Products.ToList();
        var random = new Random(55);

        if (!suppliers.Any() || !products.Any()) return;

        for (int i = 0; i < 15; i++)
        {
            var supplier = suppliers[random.Next(suppliers.Count)];
            var po = PurchaseOrder.Create(supplier.Id);

            int itemsCount = random.Next(1, 5);
            for (int j = 0; j < itemsCount; j++)
            {
                var product = products[random.Next(products.Count)];
                var quantity = random.Next(10, 100);
                var unitCost = Math.Round(product.SellingPrice * 0.6m, 2);
                var expiryDate = DateTime.UtcNow.AddMonths(random.Next(1, 24));
                po.AddItem(product, quantity, unitCost, expiryDate);
            }

            po.Complete();
            context.Set<PurchaseOrder>().Add(po);
        }

        context.SaveChanges();
    }
}
