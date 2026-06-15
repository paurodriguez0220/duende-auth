using MediatR;
using ScalarApi.Common.Constants;
using ScalarApi.Features.Forecasts.CreateForecast;
using ScalarApi.Features.Forecasts.GetForecasts;

namespace ScalarApi.Features.Forecasts;

public static class ForecastEndpoints
{
    public static void MapForecastEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/forecasts")
            .WithTags("Forecasts")
            .RequireAuthorization()
            .RequireRateLimiting(PolicyNames.RateLimiterPolicy);

        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetForecastsQuery(), ct)))
            .WithName("GetForecasts")
            .WithSummary("Get all weather forecasts")
            .Produces<IReadOnlyList<ForecastDto>>();

        group.MapPost("/", async (CreateForecastCommand command, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/v1/forecasts/{result.Id}", result);
            })
            .WithName("CreateForecast")
            .WithSummary("Create a weather forecast")
            .Produces<ForecastDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }
}
