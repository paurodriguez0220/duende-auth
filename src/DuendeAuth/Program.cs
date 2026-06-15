using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using DuendeAuth;
using DuendeAuth.Admin;
using DuendeAuth.Common.Constants;
using DuendeAuth.Data;
using DuendeAuth.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) =>
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Identity));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var identityServer = builder.Services
    .AddIdentityServer()
    .AddAspNetIdentity<IdentityUser>()
    .AddConfigurationStore(options =>
        options.ConfigureDbContext = b =>
            b.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Config, ConnectionStringNames.ConfigMigrationsAssembly))
    .AddOperationalStore(options =>
        options.ConfigureDbContext = b =>
            b.UseConfiguredProvider(builder.Configuration, ConnectionStringNames.Grants, ConnectionStringNames.GrantsMigrationsAssembly));

if (builder.Environment.IsDevelopment())
    identityServer.AddDeveloperSigningCredential();
else
    throw new InvalidOperationException(
        "A signing certificate must be configured for non-development environments. Call AddSigningCredential() with a valid certificate.");

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration[ConfigKeys.Authority];
        options.TokenValidationParameters.ValidAudiences = [Scopes.DuendeManage, Scopes.DuendeRead];
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.Admin, policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireClaim("scope", Scopes.DuendeManage);
    });
    options.AddPolicy(PolicyNames.AdminRead, policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireClaim("scope", Scopes.DuendeManage, Scopes.DuendeRead);
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(PolicyNames.RateLimit, opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests.",
            Type = "https://tools.ietf.org/html/rfc6585#section-4"
        }, ct);
    };
});

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        var authority = builder.Configuration[ConfigKeys.Authority];
        document.Info = new OpenApiInfo { Title = "DuendeAuth", Version = "v1" };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
        {
            [PolicyNames.OAuthSecurityScheme] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"{authority}/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            [Scopes.DuendeManage] = "Full management access (users, claims, clients)"
                        }
                    }
                }
            }
        };
        return Task.CompletedTask;
    }));

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(title: "An unexpected error occurred.", statusCode: 500)
            .ExecuteAsync(context);
    }));

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnostics, context) =>
    {
        diagnostics.Set("UserId", context.User?.FindFirst("sub")?.Value);
        diagnostics.Set("ClientIp", context.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseRateLimiter();

await SeedData.InitializeAsync(app.Services, default);

app.UseIdentityServer();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference(options =>
    options.WithTitle("DuendeAuth")
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
           .AddPreferredSecuritySchemes(PolicyNames.OAuthSecurityScheme));

app.MapAdminEndpoints();

app.Run();
