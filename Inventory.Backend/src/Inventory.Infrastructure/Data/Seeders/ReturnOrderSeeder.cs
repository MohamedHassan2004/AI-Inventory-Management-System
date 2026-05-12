using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Inventory.Infrastructure.Data.Seeders;

public class ReturnOrderSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Set<ReturnOrder>().Any()) return;

        var completedOrders = context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.Status == OrderStatus.Completed)
            .ToList();
            
        var random = new Random(66);

        if (!completedOrders.Any()) return;

        for (int i = 0; i < 10; i++)
        {
            var order = completedOrders[random.Next(completedOrders.Count)];
            if (!order.Items.Any()) continue;

            var reason = random.Next(0, 2) == 0 ? "Defective" : "Customer changed mind";
            
            var ro = new ReturnOrder(order, order.CashierId, reason);

            int itemsCount = random.Next(1, Math.Max(2, order.Items.Count));
            var itemsToReturn = order.Items.Take(itemsCount).ToList();
            
            foreach (var item in itemsToReturn)
            {
                decimal quantity = random.Next(1, (int)Math.Max(2, (int)item.Quantity));
                if (quantity > item.Quantity) quantity = item.Quantity;
                
                var expiry = DateTime.UtcNow.AddMonths(1);
                try 
                {
                    ro.AddItem(item, quantity, expiry);
                } 
                catch 
                { 
                    // Skip if duplicate
                }
            }

            try 
            {
                ro.Process();
                context.Set<ReturnOrder>().Add(ro);
            }
            catch 
            {
                // Skip if empty or error
            }
        }

        context.SaveChanges();
    }
}
