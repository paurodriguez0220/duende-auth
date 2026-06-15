using FluentValidation;

namespace ScalarApi.Features.Forecasts.CreateForecast;

public class CreateForecastValidator : AbstractValidator<CreateForecastCommand>
{
    public CreateForecastValidator()
    {
        RuleFor(x => x.TemperatureC).InclusiveBetween(-90, 60).WithMessage("Temperature must be between -90 and 60 °C");
        RuleFor(x => x.Summary).MaximumLength(200).When(x => x.Summary is not null);
    }
}
