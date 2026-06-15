using Scalar.AspNetCore;
using ScalarApi.Common.Constants;
using ScalarApi.Features.Forecasts;
using ScalarApi.Infrastructure.Data;
using Serilog;

namespace ScalarApi.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    }

    public static WebApplication UseApiMiddleware(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseCors(PolicyNames.CorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
            options.WithTitle("ScalarApi")
                   .WithTheme(ScalarTheme.Purple)
                   .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
        app.MapHealthChecks("/health");
        app.MapForecastEndpoints();
        return app;
    }
}
