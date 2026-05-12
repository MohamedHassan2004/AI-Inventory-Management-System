using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Inventory.Infrastructure.Data.Seeders;

public class ProductSeeder
{
    public static void Seed(ApplicationDbContext context)
    {
        if (context.Products.Any()) return;

        var categories = context.Categories.ToList();
        var suppliers = context.Suppliers.ToList();
        var random = new Random(42);

        if (!categories.Any() || !suppliers.Any()) return;

        var productsList = new[]
        {
            "Whole Milk", "Cheddar Cheese", "Sliced Bread", "Eggs (1 Dozen)", "Butter",
            "Chicken Breast", "Ground Beef", "Apples", "Bananas", "Oranges",
            "Potatoes", "Onions", "Tomatoes", "Carrots", "Broccoli",
            "Spinach", "Lettuce", "Cucumber", "Bell Peppers", "Garlic",
            "Avocados", "Strawberries", "Blueberries", "Grapes", "Watermelon",
            "Lemons", "Limes", "Cereal", "Oatmeal", "Pancake Mix",
            "Maple Syrup", "Peanut Butter", "Jelly", "Honey", "Coffee",
            "Tea Bags", "Orange Juice", "Apple Juice", "Bottled Water", "Soda (12 Pack)",
            "Pasta", "Tomato Sauce", "Rice", "Black Beans", "Canned Corn",
            "Tuna", "Olive Oil", "Vegetable Oil", "Vinegar", "Salt",
            "Black Pepper", "Sugar", "Flour", "Baking Powder", "Baking Soda",
            "Vanilla Extract", "Ketchup", "Mustard", "Mayonnaise", "Soy Sauce",
            "Hot Sauce", "Salad Dressing", "Potato Chips", "Tortilla Chips", "Salsa"
        };

        var addedProducts = new List<Product>();

        for (int i = 0; i < productsList.Length; i++)
        {
            var sellingPrice = Math.Round((decimal)(random.Next(10, 500) + random.NextDouble()), 2);
            var product = new Product(
                sku: $"SKU-{(i + 1):000}",
                name: productsList[i],
                sellingPrice: sellingPrice,
                reorderPoint: random.Next(5, 20)
            );

            var category = categories[random.Next(categories.Count)];
            product.AssignCategory(category.Id);

            context.Products.Add(product);
            addedProducts.Add(product);
        }

        context.SaveChanges();

        foreach (var product in addedProducts)
        {
            var supplier = suppliers[random.Next(suppliers.Count)];
            product.AddStock(
                supplierId: supplier.Id,
                expiryDate: DateTime.UtcNow.AddMonths(random.Next(1, 12)),
                unitCost: Math.Round(product.SellingPrice * 0.6m, 2),
                quantity: random.Next(50, 200)
            );
        }

        context.SaveChanges();
    }
}