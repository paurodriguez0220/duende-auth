using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using ScalarApi.Common.Behaviors;
using ScalarApi.Common.Exceptions;
using ScalarApi.Features.Forecasts;
using ScalarApi.Infrastructure.Data;
using Serilog;
using System.Reflection;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

    // EF Core + SQLite
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // MediatR (CQRS)
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

    // FluentValidation + pipeline behavior
    builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    // JWT auth — trusts tokens issued by DuendeAuth
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Auth:Authority"];
            options.TokenValidationParameters.ValidateAudience = false;
        });

    builder.Services.AddAuthorization();

    // CORS
    builder.Services.AddCors(o =>
        o.AddPolicy("AllowAll", p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    // Rate limiting — 100 req/min per endpoint group
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 5;
        });
    });

    // Health checks
    builder.Services.AddHealthChecks();

    // Global exception handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // OpenAPI with OAuth2 security scheme for Scalar
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer(async (document, context, ct) =>
        {
            var authority = builder.Configuration["Auth:Authority"];
            document.Info.Title = "ScalarApi";
            document.Info.Version = "v1";
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes["oauth2"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"{authority}/connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["scalar-api"] = "Access Scalar API"
                        }
                    }
                }
            };
            await Task.CompletedTask;
        });
    });

    var app = builder.Build();

    // Ensure DB schema exists
    using (var scope = app.Services.CreateScope())
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseCors("AllowAll");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
        options.WithTitle("ScalarApi")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));

    app.MapHealthChecks("/health");
    app.MapForecastEndpoints();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}
