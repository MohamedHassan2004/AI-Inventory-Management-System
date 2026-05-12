using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Infrastructure.Data.Seeders;

public class OrderSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Orders.Any()) return;

        var products = context.Products.Include(p => p.Batches).ToList();
        var cashiers = context.Users.Where(u => u.Email != null && u.Email.Contains("cashier")).ToList();
        var random = new Random(1337);

        if (!products.Any() || !cashiers.Any()) return;

        for (int i = 1; i <= 90; i++)
        {
            var cashier = cashiers[random.Next(cashiers.Count)];
            
            int itemCount = random.Next(1, 4);
            var orderItems = new List<(Product product, decimal quantity)>();
            
            for (int j = 0; j < itemCount; j++)
            {
                var product = products[random.Next(products.Count)];
                // Avoid duplicates in the same order
                if (!orderItems.Any(oi => oi.product.Id == product.Id))
                {
                    decimal quantity = random.Next(1, 5);
                    orderItems.Add((product, quantity));
                }
            }

            if (!orderItems.Any()) continue;

            var types = Enum.GetValues<OrderType>();
            var type = types[random.Next(types.Length)];

            var methods = Enum.GetValues<PaymentMethod>();
            var payment = methods[random.Next(methods.Length)];

            try 
            {
                var order = Order.Submit(
                    cashierId: cashier.Id,
                    items: orderItems,
                    paymentMethod: payment,
                    orderType: type,
                    discountPercentage: 0
                );
                
                context.Orders.Add(order);
            }
            catch(Exception)
            {
                // Proceed if stock is insufficient
                continue;
            }
        }

        context.SaveChanges();
    }
}