using MediatR;
using Microsoft.EntityFrameworkCore;
using ScalarApi.Infrastructure.Data;

namespace ScalarApi.Features.Forecasts.GetForecasts;

public class GetForecastsHandler(AppDbContext db)
    : IRequestHandler<GetForecastsQuery, IReadOnlyList<ForecastDto>>
{
    public async Task<IReadOnlyList<ForecastDto>> Handle(GetForecastsQuery request, CancellationToken cancellationToken) =>
        await db.Forecasts
            .Select(f => new ForecastDto(f.Id, f.Date, f.TemperatureC, f.TemperatureF, f.Summary))
            .ToListAsync(cancellationToken);
}
