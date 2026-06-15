namespace ScalarApi.Common.Models;

public record ErrorDetail(string Field, string Message);

public record ApiError(string Code, string Message, IReadOnlyList<ErrorDetail> Details);

public record ErrorResponse(ApiError Error);
