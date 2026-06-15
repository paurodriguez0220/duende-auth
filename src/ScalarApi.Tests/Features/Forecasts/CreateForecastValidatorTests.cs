using FluentAssertions;
using ScalarApi.Features.Forecasts.CreateForecast;

namespace ScalarApi.Tests.Features.Forecasts;

public class CreateForecastValidatorTests
{
    private readonly CreateForecastValidator _validator = new();

    [Fact]
    public void Validate_IsValid_WhenCommandIsValid()
    {
        var command = new CreateForecastCommand(new DateOnly(2026, 1, 1), 20, "Sunny");
        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(100)]
    public void Validate_Fails_WhenTemperatureIsOutOfRange(int temperature)
    {
        var command = new CreateForecastCommand(new DateOnly(2026, 1, 1), temperature, null);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "TemperatureC");
    }

    [Fact]
    public void Validate_Fails_WhenSummaryExceedsMaxLength()
    {
        var command = new CreateForecastCommand(new DateOnly(2026, 1, 1), 20, new string('x', 201));
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Summary");
    }

    [Fact]
    public void Validate_IsValid_WhenSummaryIsNull()
    {
        var command = new CreateForecastCommand(new DateOnly(2026, 1, 1), 20, null);
        _validator.Validate(command).IsValid.Should().BeTrue();
    }
}
