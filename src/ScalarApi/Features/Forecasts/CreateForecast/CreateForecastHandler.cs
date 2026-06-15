using MediatR;
using ScalarApi.Domain.Entities;
using ScalarApi.Features.Forecasts.GetForecasts;
using ScalarApi.Infrastructure.Data;

namespace ScalarApi.Features.Forecasts.CreateForecast;

public class CreateForecastHandler(AppDbContext db)
    : IRequestHandler<CreateForecastCommand, ForecastDto>
{
    public async Task<ForecastDto> Handle(CreateForecastCommand request, CancellationToken cancellationToken)
    {
        var forecast = new Forecast
        {
            Date = request.Date,
            TemperatureC = request.TemperatureC,
            Summary = request.Summary
        };

        db.Forecasts.Add(forecast);
        await db.SaveChangesAsync(cancellationToken);

        return new ForecastDto(forecast.Id, forecast.Date, forecast.TemperatureC, forecast.TemperatureF, forecast.Summary);
    }
}
