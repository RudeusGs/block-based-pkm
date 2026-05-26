using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Pkm.Api.Contracts.Common;

namespace Pkm.Api.Common.Errors;

public static class ApiModelStateResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var details = GetValidationErrors(context.ModelState);

        var response = ApiResult.Failure(
            message: "Dữ liệu đầu vào không hợp lệ.",
            error: new ApiError(
                Code: "Api.ValidationFailed",
                Type: "validation_error",
                Details: details),
            statusCode: StatusCodes.Status400BadRequest,
            traceId: traceId);

        return new BadRequestObjectResult(response);
    }

    private static IReadOnlyList<string> GetValidationErrors(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error => FormatError(entry.Key, error)))
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return errors.Length == 0
            ? new[] { "Request body hoặc query string không hợp lệ." }
            : errors;
    }

    private static string FormatError(string field, ModelError error)
    {
        var message = string.IsNullOrWhiteSpace(error.ErrorMessage)
            ? "Giá trị không hợp lệ."
            : error.ErrorMessage;

        return string.IsNullOrWhiteSpace(field)
            ? message
            : $"{field}: {message}";
    }
}
