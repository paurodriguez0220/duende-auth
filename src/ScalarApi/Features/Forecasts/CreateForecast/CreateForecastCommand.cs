using MediatR;
using ScalarApi.Features.Forecasts.GetForecasts;

namespace ScalarApi.Features.Forecasts.CreateForecast;

public record CreateForecastCommand(DateOnly Date, int TemperatureC, string? Summary)
    : IRequest<ForecastDto>;
