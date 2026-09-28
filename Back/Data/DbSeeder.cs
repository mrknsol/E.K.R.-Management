using EKR.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EKR.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, "superadmin@ekr.local", "Super Admin", "SuperAdmin123!", AppRoles.SuperAdmin);
        await EnsureUserAsync(userManager, "admin@ekr.local", "Warehouse Admin", "Admin123!", AppRoles.Admin);
        await EnsureUserAsync(userManager, "factory@ekr.local", "Factory Worker", "Factory123!", AppRoles.Factory);

        if (!await db.Products.AnyAsync())
        {
            var product = new Product
            {
                ModelName = "EKR Classic Jacket",
                Description = "Базовая куртка для примера",
                Variants =
                [
                    new ProductVariant { Color = "Black", Size = "M", StockQuantity = 50 },
                    new ProductVariant { Color = "Black", Size = "L", StockQuantity = 40 },
                    new ProductVariant { Color = "Navy", Size = "M", StockQuantity = 35 },
                    new ProductVariant { Color = "Navy", Size = "L", StockQuantity = 30 }
                ]
            };

            db.Products.Add(product);
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string fullName,
        string password,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return;
        }

        user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRoleAsync(user, role);
    }
}
