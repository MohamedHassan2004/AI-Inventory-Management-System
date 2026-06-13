using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Data.Seeders;

public class OrderSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Orders.Any()) return;

        var products = context.Products
            .Include(p => p.Batches)
            .ToList();

        var cashiers = context.Users
            .Where(u => u.Email != null && u.Email.Contains("cashier"))
            .ToList();

        var random = new Random(1337);

        if (!products.Any() || !cashiers.Any())
            return;

        Product P(string name) => products.First(p => p.Name == name);

        var patterns = new List<List<(Product product, decimal qty)>>
        {
            new()
            {
                (P("Coffee"),2),
                (P("Sugar"),1),
                (P("Whole Milk"),1)
            },

            new()
            {
                (P("Tea Bags"),2),
                (P("Sugar"),1),
                (P("Honey"),1)
            },

            new()
            {
                (P("Sliced Bread"),2),
                (P("Butter"),1),
                (P("Cheddar Cheese"),1)
            },

            new()
            {
                (P("Sliced Bread"),2),
                (P("Peanut Butter"),1),
                (P("Jelly"),1)
            },

            new()
            {
                (P("Pasta"),2),
                (P("Tomato Sauce"),1),
                (P("Olive Oil"),1)
            },

            new()
            {
                (P("Chicken Breast"),2),
                (P("Potatoes"),2),
                (P("Onions"),1)
            },

            new()
            {
                (P("Ground Beef"),2),
                (P("Tomatoes"),1),
                (P("Onions"),1)
            },

            new()
            {
                (P("Lettuce"),1),
                (P("Tomatoes"),1),
                (P("Cucumber"),1)
            },

            new()
            {
                (P("Apples"),2),
                (P("Bananas"),2),
                (P("Orange Juice"),1)
            },

            new()
            {
                (P("Potato Chips"),2),
                (P("Soda (12 Pack)"),1)
            },

            new()
            {
                (P("Tortilla Chips"),2),
                (P("Salsa"),1),
                (P("Soda (12 Pack)"),1)
            },

            new()
            {
                (P("Flour"),2),
                (P("Sugar"),2),
                (P("Baking Powder"),1),
                (P("Vanilla Extract"),1)
            },

            new()
            {
                (P("Rice"),2),
                (P("Chicken Breast"),2),
                (P("Vegetable Oil"),1)
            }
        };

        int totalOrders = 200;

        for (int i = 0; i < totalOrders; i++)
        {
            try
            {
                var cashier = cashiers[random.Next(cashiers.Count)];

                List<(Product product, decimal quantity)> items;

                bool usePattern = random.NextDouble() < 0.7;

                if (usePattern)
                {
                    var pattern = patterns[random.Next(patterns.Count)];

                    items = pattern
                        .Select(x => (
                            x.product,
                            x.qty + random.Next(0, 3)
                        ))
                        .ToList();
                }
                else
                {
                    items = new();

                    int count = random.Next(1, 5);

                    while (items.Count < count)
                    {
                        var product = products[random.Next(products.Count)];

                        if (items.Any(x => x.product.Id == product.Id))
                            continue;

                        items.Add((
                            product,
                            random.Next(1, 5)
                        ));
                    }
                }

                var paymentMethods = Enum.GetValues<PaymentMethod>();
                var payment =
                    paymentMethods[random.Next(paymentMethods.Length)];

                var orderTypes = Enum.GetValues<OrderType>();
                var type =
                    orderTypes[random.Next(orderTypes.Length)];

                var order = Order.Submit(
                    cashierId: cashier.Id,
                    items: items,
                    paymentMethod: payment,
                    orderType: type,
                    discountPercentage: random.Next(0, 15)
                );

                order.SetOrderDate(
                    DateTime.UtcNow.AddDays(
                        -random.Next(0, 60)
                    )
                );

                context.Orders.Add(order);
            }
            catch
            {
                continue;
            }
        }

        context.SaveChanges();
    }
}


//using Inventory.Domain.Entities;
//using Inventory.Domain.Enums;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Inventory.Infrastructure.Data.Seeders;

//public class OrderSeeder
//{
//    public static void Seed(ApplicationDbContext context)
//    {
//        if (context.Orders.Any()) return;

//        var products = context.Products.Include(p => p.Batches).ToList();
//        var cashiers = context.Users.Where(u => u.Email != null && u.Email.Contains("cashier")).ToList();
//        var random = new Random(1337);

//        if (!products.Any() || !cashiers.Any()) return;

//        for (int i = 1; i <= 90; i++)
//        {
//            var cashier = cashiers[random.Next(cashiers.Count)];

//            int itemCount = random.Next(1, 4);
//            var orderItems = new List<(Product product, decimal quantity)>();

//            for (int j = 0; j < itemCount; j++)
//            {
//                var product = products[random.Next(products.Count)];
//                // Avoid duplicates in the same order
//                if (!orderItems.Any(oi => oi.product.Id == product.Id))
//                {
//                    decimal quantity = random.Next(1, 5);
//                    orderItems.Add((product, quantity));
//                }
//            }

//            if (!orderItems.Any()) continue;

//            var types = Enum.GetValues<OrderType>();
//            var type = types[random.Next(types.Length)];

//            var methods = Enum.GetValues<PaymentMethod>();
//            var payment = methods[random.Next(methods.Length)];

//            try 
//            {
//                var order = Order.Submit(
//                    cashierId: cashier.Id,
//                    items: orderItems,
//                    paymentMethod: payment,
//                    orderType: type,
//                    discountPercentage: 0
//                );

//                context.Orders.Add(order);
//            }
//            catch(Exception)
//            {
//                // Proceed if stock is insufficient
//                continue;
//            }
//        }

//        context.SaveChanges();
//    }
//}