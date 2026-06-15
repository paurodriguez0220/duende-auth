using Duende.IdentityServer.EntityFramework.DbContexts;
using DuendeAuth.Common.Constants;
using DuendeAuth.Data;
using Microsoft.AspNetCore.Identity;

namespace DuendeAuth;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        await sp.GetRequiredService<PersistedGrantDbContext>().Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var config = sp.GetRequiredService<IConfiguration>();
        var adminPassword = config[ConfigKeys.AdminPassword]
            ?? throw new InvalidOperationException($"{ConfigKeys.AdminPassword} is not configured.");

        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        if (await userManager.FindByNameAsync(SeedDefaults.AdminUserName).ConfigureAwait(false) is null)
        {
            var admin = new IdentityUser
            {
                UserName = SeedDefaults.AdminUserName,
                Email = SeedDefaults.AdminEmail,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, adminPassword).ConfigureAwait(false);
        }
    }
}
