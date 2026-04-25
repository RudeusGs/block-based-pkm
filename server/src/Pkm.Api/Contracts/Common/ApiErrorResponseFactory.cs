using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pkm.Api.Contracts.Common;
using Pkm.Application.Common.Results;
using Pkm.Domain.Common;

namespace Pkm.Api.Common;

public static class ApiErrorResponseFactory
{
    public static ApiResult FromApplicationError(Error error, string traceId)
    {
        var descriptor = Resolve(error.Status);

        return ApiResult.Failure(
            message: error.Message,
            error: new ApiError(
                Code: error.Code,
                Type: descriptor.Type,
                Details: error.Details),
            statusCode: descriptor.StatusCode,
            traceId: traceId);
    }

    public static ApiResult FromException(Exception exception, string traceId)
    {
        var descriptor = exception switch
        {
            DbUpdateConcurrencyException => new ErrorDescriptor(
                StatusCodes.Status409Conflict,
                "conflict",
                "Persistence.ConcurrencyConflict",
                "Dữ liệu đã thay đổi bởi một thao tác khác. Vui lòng tải lại và thử lại.",
                Array.Empty<string>()),

            DbUpdateException dbUpdateException when IsPostgresUniqueViolation(dbUpdateException) => new ErrorDescriptor(
                StatusCodes.Status409Conflict,
                "conflict",
                "Persistence.UniqueConstraintViolation",
                "Dữ liệu bị trùng hoặc đã thay đổi bởi một thao tác khác. Vui lòng tải lại và thử lại.",
                Array.Empty<string>()),

            DomainException => new ErrorDescriptor(
                StatusCodes.Status422UnprocessableEntity,
                "domain_error",
                "Domain.RuleViolation",
                exception.Message,
                Array.Empty<string>()),

            UnauthorizedAccessException => new ErrorDescriptor(
                StatusCodes.Status401Unauthorized,
                "unauthorized",
                "Auth.Unauthorized",
                exception.Message,
                Array.Empty<string>()),

            _ => new ErrorDescriptor(
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "System.InternalError",
                "Đã xảy ra lỗi hệ thống ngoài dự kiến.",
                Array.Empty<string>())
        };

        return ApiResult.Failure(
            message: descriptor.Message,
            error: new ApiError(
                Code: descriptor.Code,
                Type: descriptor.Type,
                Details: descriptor.Details),
            statusCode: descriptor.StatusCode,
            traceId: traceId);
    }

    private static bool IsPostgresUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };

    private static ErrorDescriptor Resolve(ResultStatus status)
        => status switch
        {
            ResultStatus.Validation => new(StatusCodes.Status400BadRequest, "validation_error", "", "", Array.Empty<string>()),
            ResultStatus.Unauthorized => new(StatusCodes.Status401Unauthorized, "unauthorized", "", "", Array.Empty<string>()),
            ResultStatus.Forbidden => new(StatusCodes.Status403Forbidden, "forbidden", "", "", Array.Empty<string>()),
            ResultStatus.NotFound => new(StatusCodes.Status404NotFound, "not_found", "", "", Array.Empty<string>()),
            ResultStatus.Conflict => new(StatusCodes.Status409Conflict, "conflict", "", "", Array.Empty<string>()),
            ResultStatus.Unprocessable => new(StatusCodes.Status422UnprocessableEntity, "unprocessable_entity", "", "", Array.Empty<string>()),
            _ => new(StatusCodes.Status500InternalServerError, "internal_error", "", "", Array.Empty<string>())
        };

    private sealed record ErrorDescriptor(
        int StatusCode,
        string Type,
        string Code,
        string Message,
        IReadOnlyList<string> Details);
}