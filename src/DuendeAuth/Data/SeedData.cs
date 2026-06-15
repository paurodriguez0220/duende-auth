using Duende.IdentityServer.EntityFramework.DbContexts;
using DuendeAuth.Data;
using Microsoft.AspNetCore.Identity;

namespace DuendeAuth;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
        await sp.GetRequiredService<PersistedGrantDbContext>().Database.EnsureCreatedAsync();

        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        if (await userManager.FindByNameAsync("admin") is null)
        {
            var admin = new IdentityUser { UserName = "admin", Email = "admin@example.com", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "Admin1234!");
        }
    }
}
