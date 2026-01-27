using Inventory.Domain.Entities.Users;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Infrastructure.Data
{
    public class ApplicationDbContextSeed
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            var roles = Enum.GetNames<UserRole>();

            foreach (var role in roles)
            {
                var roleExist = await roleManager.RoleExistsAsync(role);
                if (!roleExist)
                {
                    var identityRole = new IdentityRole(role);
                    await roleManager.CreateAsync(identityRole);
                }
            }
        }

        public static async Task SeedSuperAdminUserAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser(
                userName: "super.admin@pos.com",
                fullName: "System Super Admin",
                email: "super.admin@pos.com",
                phoneNumber: "0123456789",
                identityImgUrl: "default-admin.png",
                role: UserRole.SuperAdmin
            );

            if (userManager.Users.All(u => u.Id != defaultUser.Id))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email!);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "AdminPassword123!");
                    await userManager.AddToRoleAsync(defaultUser, UserRole.SuperAdmin.ToString());
                }
            }
        }
    }
}
