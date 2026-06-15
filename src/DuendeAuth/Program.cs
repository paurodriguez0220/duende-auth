using DuendeAuth;
using DuendeAuth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connString = builder.Configuration.GetConnectionString("DefaultConnection")!;

builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlite(connString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddIdentityServer()
    .AddAspNetIdentity<IdentityUser>()
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryApiResources(Config.ApiResources)
    .AddInMemoryClients(Config.Clients)
    .AddOperationalStore(options =>
        options.ConfigureDbContext = b =>
            b.UseSqlite(connString, sql =>
                sql.MigrationsAssembly("DuendeAuth")));

var app = builder.Build();

await SeedData.InitializeAsync(app.Services);

app.UseIdentityServer();

app.Run();
