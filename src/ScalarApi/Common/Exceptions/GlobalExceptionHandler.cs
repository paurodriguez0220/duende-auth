using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using ScalarApi.Common.Models;

namespace ScalarApi.Common.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);

        var (statusCode, code, message, details) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                "VALIDATION_FAILED",
                "One or more validation errors occurred.",
                (IReadOnlyList<ErrorDetail>)ve.Errors
                    .Select(e => new ErrorDetail(e.PropertyName, e.ErrorMessage))
                    .ToList()
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred.",
                (IReadOnlyList<ErrorDetail>)Array.Empty<ErrorDetail>()
            )
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new ErrorResponse(new ApiError(code, message, details)), cancellationToken);
        return true;
    }
}
