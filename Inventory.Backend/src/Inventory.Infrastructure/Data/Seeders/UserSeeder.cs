using Inventory.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Data.Seeders;

public class UserSeeder
{
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
    {
        if (userManager.Users.Any(u => u.Email == "cashier1@pos.com"))
            return;

        for (int i = 1; i <= 5; i++)
        {
            var cashier = new ApplicationUser(
                userName: $"cashier{i}",
                fullName: $"Cashier User {i}",
                email: $"cashier{i}@pos.com",
                phoneNumber: $"010000000{i:D2}"
            );

            cashier.ApproveAccount();

            var result = await userManager.CreateAsync(
                cashier,
                "Password123!"
            );

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(
                    cashier,
                    "Cashier"
                );
            }
        }
    }
}

//using Inventory.Domain.Entities.Users;
//using Microsoft.AspNetCore.Identity;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Inventory.Infrastructure.Data.Seeders;

//public class UserSeeder
//{
//    public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
//    {
//        if (userManager.Users.Any(u => u.Email == "cashier1@pos.com"))
//            return;

//        for (int i = 1; i <= 5; i++)
//        {
//            var cashier = new ApplicationUser(
//                userName: $"cashier{i}",
//                fullName: $"Cashier User {i}",
//                email: $"cashier{i}@pos.com",
//                phoneNumber: $"010000000{i:D2}"
//            );
//            cashier.ApproveAccount();
//            await userManager.CreateAsync(cashier, "Password123!");
//        }
//    }
//}
