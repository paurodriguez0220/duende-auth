using MediatR;

namespace ScalarApi.Features.Forecasts.GetForecasts;

public record GetForecastsQuery : IRequest<IReadOnlyList<ForecastDto>>;

public record ForecastDto(int Id, DateOnly Date, int TemperatureC, int TemperatureF, string? Summary);
