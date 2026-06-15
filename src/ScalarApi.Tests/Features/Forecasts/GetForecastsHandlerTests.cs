using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ScalarApi.Domain.Entities;
using ScalarApi.Features.Forecasts.GetForecasts;
using ScalarApi.Infrastructure.Data;

namespace ScalarApi.Tests.Features.Forecasts;

public class GetForecastsHandlerTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private GetForecastsHandler _handler = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();

        _handler = new GetForecastsHandler(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoForecastsExist()
    {
        var result = await _handler.Handle(new GetForecastsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsMappedDtos_WhenForecastsExist()
    {
        _db.Forecasts.Add(new Forecast { Date = new DateOnly(2026, 1, 1), TemperatureC = 20, Summary = "Sunny" });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetForecastsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].TemperatureC.Should().Be(20);
        result[0].Summary.Should().Be("Sunny");
    }
}
