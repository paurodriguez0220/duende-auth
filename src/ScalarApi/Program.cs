using ScalarApi.Infrastructure.Extensions;
using Serilog;

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

    builder.Services
        .AddPersistence(builder.Configuration)
        .AddMediator()
        .AddApiAuth(builder.Configuration)
        .AddApiCors()
        .AddApiRateLimiting()
        .AddApiHealthChecks()
        .AddGlobalExceptionHandling()
        .AddOpenApiWithSecurity(builder.Configuration);

    var app = builder.Build();

    await app.InitializeDatabaseAsync();
    app.UseApiMiddleware();
    app.MapApiEndpoints();

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
