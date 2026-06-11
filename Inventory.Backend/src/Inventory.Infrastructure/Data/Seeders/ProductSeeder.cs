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

        var categoryMap = new Dictionary<string, string>
        {
            // Beverages
            ["Coffee"] = "Beverages",
            ["Tea Bags"] = "Beverages",
            ["Orange Juice"] = "Beverages",
            ["Apple Juice"] = "Beverages",
            ["Bottled Water"] = "Beverages",
            ["Soda (12 Pack)"] = "Beverages",

            // Dairy
            ["Whole Milk"] = "Dairy",
            ["Cheddar Cheese"] = "Dairy",
            ["Eggs (1 Dozen)"] = "Dairy",
            ["Butter"] = "Dairy",

            // Meat
            ["Chicken Breast"] = "Meat",
            ["Ground Beef"] = "Meat",

            // Bakery
            ["Sliced Bread"] = "Bakery",
            ["Pancake Mix"] = "Bakery",
            ["Flour"] = "Bakery",
            ["Baking Powder"] = "Bakery",
            ["Baking Soda"] = "Bakery",
            ["Vanilla Extract"] = "Bakery",

            // Snacks
            ["Peanut Butter"] = "Snacks",
            ["Jelly"] = "Snacks",
            ["Potato Chips"] = "Snacks",
            ["Tortilla Chips"] = "Snacks",
            ["Salsa"] = "Snacks",

            // Fruits
            ["Apples"] = "Fruits",
            ["Bananas"] = "Fruits",
            ["Oranges"] = "Fruits",
            ["Avocados"] = "Fruits",
            ["Strawberries"] = "Fruits",
            ["Blueberries"] = "Fruits",
            ["Grapes"] = "Fruits",
            ["Watermelon"] = "Fruits",
            ["Lemons"] = "Fruits",
            ["Limes"] = "Fruits",

            // Vegetables
            ["Potatoes"] = "Vegetables",
            ["Onions"] = "Vegetables",
            ["Tomatoes"] = "Vegetables",
            ["Carrots"] = "Vegetables",
            ["Broccoli"] = "Vegetables",
            ["Spinach"] = "Vegetables",
            ["Lettuce"] = "Vegetables",
            ["Cucumber"] = "Vegetables",
            ["Bell Peppers"] = "Vegetables",
            ["Garlic"] = "Vegetables"
        };

        var prices = new Dictionary<string, decimal>
        {
            ["Whole Milk"] = 5.99m,
            ["Cheddar Cheese"] = 8.49m,
            ["Sliced Bread"] = 3.49m,
            ["Eggs (1 Dozen)"] = 4.99m,
            ["Butter"] = 5.49m,

            ["Chicken Breast"] = 12.99m,
            ["Ground Beef"] = 14.99m,

            ["Apples"] = 2.99m,
            ["Bananas"] = 1.99m,
            ["Oranges"] = 3.49m,
            ["Potatoes"] = 2.49m,
            ["Onions"] = 1.99m,
            ["Tomatoes"] = 3.29m,
            ["Carrots"] = 2.19m,
            ["Broccoli"] = 2.99m,
            ["Spinach"] = 3.49m,
            ["Lettuce"] = 2.49m,
            ["Cucumber"] = 1.79m,
            ["Bell Peppers"] = 3.99m,
            ["Garlic"] = 1.49m,

            ["Avocados"] = 4.99m,
            ["Strawberries"] = 5.99m,
            ["Blueberries"] = 6.49m,
            ["Grapes"] = 4.49m,
            ["Watermelon"] = 8.99m,
            ["Lemons"] = 2.49m,
            ["Limes"] = 2.29m,

            ["Cereal"] = 6.99m,
            ["Oatmeal"] = 5.49m,
            ["Pancake Mix"] = 4.99m,
            ["Maple Syrup"] = 9.99m,

            ["Peanut Butter"] = 5.99m,
            ["Jelly"] = 4.49m,
            ["Honey"] = 8.99m,

            ["Coffee"] = 14.99m,
            ["Tea Bags"] = 7.99m,

            ["Orange Juice"] = 4.99m,
            ["Apple Juice"] = 4.79m,
            ["Bottled Water"] = 1.49m,
            ["Soda (12 Pack)"] = 8.99m,

            ["Pasta"] = 3.49m,
            ["Tomato Sauce"] = 2.99m,
            ["Rice"] = 6.99m,
            ["Black Beans"] = 2.49m,
            ["Canned Corn"] = 2.29m,
            ["Tuna"] = 4.99m,

            ["Olive Oil"] = 10.99m,
            ["Vegetable Oil"] = 6.99m,
            ["Vinegar"] = 3.49m,

            ["Salt"] = 1.99m,
            ["Black Pepper"] = 3.49m,
            ["Sugar"] = 3.99m,

            ["Flour"] = 3.49m,
            ["Baking Powder"] = 2.49m,
            ["Baking Soda"] = 1.99m,
            ["Vanilla Extract"] = 6.99m,

            ["Ketchup"] = 3.49m,
            ["Mustard"] = 2.99m,
            ["Mayonnaise"] = 4.49m,
            ["Soy Sauce"] = 3.99m,
            ["Hot Sauce"] = 3.49m,
            ["Salad Dressing"] = 4.99m,

            ["Potato Chips"] = 3.99m,
            ["Tortilla Chips"] = 4.49m,
            ["Salsa"] = 3.99m
        };

        var productsList = prices.Keys.ToArray();

        var addedProducts = new List<Product>();

        foreach (var productName in productsList)
        {
            var product = new Product(
                sku: $"6221000{addedProducts.Count + 1:000000}",
                name: productName,
                sellingPrice: prices[productName],
                reorderPoint: random.Next(5, 20)
            );

            var categoryName = categoryMap.ContainsKey(productName)
                ? categoryMap[productName]
                : "Pantry";

            var category = categories.First(c => c.Name == categoryName);

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
                expiryDate: DateTime.UtcNow.AddMonths(random.Next(3, 18)),
                unitCost: Math.Round(product.SellingPrice * 0.65m, 2),
                quantity: random.Next(400, 600),
                discountPercentage: 0m
            );
        }

        context.SaveChanges();
    }
}

//using Inventory.Domain.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Inventory.Infrastructure.Data.Seeders;

//public class ProductSeeder
//{
//    public static void Seed(ApplicationDbContext context)
//    {
//        if (context.Products.Any()) return;

//        var categories = context.Categories.ToList();
//        var suppliers = context.Suppliers.ToList();
//        var random = new Random(42);

//        if (!categories.Any() || !suppliers.Any()) return;

//        var productsList = new[]
//        {
//            "Whole Milk", "Cheddar Cheese", "Sliced Bread", "Eggs (1 Dozen)", "Butter",
//            "Chicken Breast", "Ground Beef", "Apples", "Bananas", "Oranges",
//            "Potatoes", "Onions", "Tomatoes", "Carrots", "Broccoli",
//            "Spinach", "Lettuce", "Cucumber", "Bell Peppers", "Garlic",
//            "Avocados", "Strawberries", "Blueberries", "Grapes", "Watermelon",
//            "Lemons", "Limes", "Cereal", "Oatmeal", "Pancake Mix",
//            "Maple Syrup", "Peanut Butter", "Jelly", "Honey", "Coffee",
//            "Tea Bags", "Orange Juice", "Apple Juice", "Bottled Water", "Soda (12 Pack)",
//            "Pasta", "Tomato Sauce", "Rice", "Black Beans", "Canned Corn",
//            "Tuna", "Olive Oil", "Vegetable Oil", "Vinegar", "Salt",
//            "Black Pepper", "Sugar", "Flour", "Baking Powder", "Baking Soda",
//            "Vanilla Extract", "Ketchup", "Mustard", "Mayonnaise", "Soy Sauce",
//            "Hot Sauce", "Salad Dressing", "Potato Chips", "Tortilla Chips", "Salsa"
//        };

//        var addedProducts = new List<Product>();

//        for (int i = 0; i < productsList.Length; i++)
//        {
//            var sellingPrice = Math.Round((decimal)(random.Next(10, 500) + random.NextDouble()), 2);
//            var product = new Product(
//                sku: $"SKU-{(i + 1):000}",
//                name: productsList[i],
//                sellingPrice: sellingPrice,
//                reorderPoint: random.Next(5, 20)
//            );

//            var category = categories[random.Next(categories.Count)];
//            product.AssignCategory(category.Id);

//            context.Products.Add(product);
//            addedProducts.Add(product);
//        }

//        context.SaveChanges();

//        foreach (var product in addedProducts)
//        {
//            var supplier = suppliers[random.Next(suppliers.Count)];
//            product.AddStock(
//                supplierId: supplier.Id,
//                expiryDate: DateTime.UtcNow.AddMonths(random.Next(1, 12)),
//                unitCost: Math.Round(product.SellingPrice * 0.6m, 2),
//                quantity: random.Next(50, 200),
//                discountPercentage: 0m
//            );
//        }

//        context.SaveChanges();
//    }
//}

