using EventHub.Domain.Constants;
using EventHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Infrastructure.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        string? superAdminEmail = null,
        string? superAdminPassword = null,
        string? superAdminFirstName = null,
        string? superAdminLastName = null)
    {
        await SeedRolesAsync(services);
        await SeedSuperAdminAsync(services, superAdminEmail, superAdminPassword, superAdminFirstName, superAdminLastName);
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
            }
        }
    }

    private static async Task SeedSuperAdminAsync(
        IServiceProvider services,
        string? email,
        string? password,
        string? firstName,
        string? lastName)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var superAdminEmail = string.IsNullOrWhiteSpace(email) ? "yossefwaeel2005@gmail.com" : email;
        var superAdminPassword = string.IsNullOrWhiteSpace(password) ? "Yossefwaeel2005@" : password;

        if (await userManager.FindByEmailAsync(superAdminEmail) is not null)
        {
            return;
        }

        var superAdmin = new ApplicationUser
        {
            UserName = superAdminEmail,
            Email = superAdminEmail,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? "Youssef" : firstName,
            LastName = string.IsNullOrWhiteSpace(lastName) ? "Waeel" : lastName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
        }
    }
}
