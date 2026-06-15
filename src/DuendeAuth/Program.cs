using DuendeAuth;
using DuendeAuth.Common.Constants;
using DuendeAuth.Data;
using DuendeAuth.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Identity));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddIdentityServer()
    .AddAspNetIdentity<IdentityUser>()
    .AddDeveloperSigningCredential()
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.GetClients(builder.Configuration))
    .AddOperationalStore(options =>
        options.ConfigureDbContext = b =>
            b.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Grants, ConnectionStringNames.GrantsMigrationsAssembly));

var app = builder.Build();

await SeedData.InitializeAsync(app.Services, default);

app.UseIdentityServer();

app.Run();
