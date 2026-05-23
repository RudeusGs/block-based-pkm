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

            DbUpdateException dbUpdateException when dbUpdateException.InnerException is PostgresException pg
                => MapPostgresException(pg),

            DbUpdateException dbUpdateException when dbUpdateException.InnerException is NpgsqlException npg
                => MapNpgsqlException(npg),

            PostgresException pgEx => MapPostgresException(pgEx),

            NpgsqlException npgEx => MapNpgsqlException(npgEx),

            InvalidOperationException invalidOperationException
                when TryFindPostgresException(invalidOperationException, out var wrappedPostgres)
                => MapPostgresException(wrappedPostgres!),

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

    private static bool TryFindPostgresException(Exception exception, out PostgresException? postgresException)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                postgresException = postgres;
                return true;
            }
        }

        postgresException = null;
        return false;
    }

    private static bool IsPostgresUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };

    private static ErrorDescriptor MapPostgresException(PostgresException pg)
        => pg.SqlState switch
        {
            PostgresErrorCodes.InvalidPassword
                or "28P01" => new ErrorDescriptor(
                    StatusCodes.Status503ServiceUnavailable,
                    "service_unavailable",
                    "Persistence.DatabaseAuthFailed",
                    "PostgreSQL từ chối đăng nhập (sai mật khẩu hoặc tên người dùng). Kiểm tra ConnectionStrings trong appsettings hoặc biến môi trường.",
                    Array.Empty<string>()),

            PostgresErrorCodes.InvalidCatalogName
                or "3D000" => new ErrorDescriptor(
                    StatusCodes.Status503ServiceUnavailable,
                    "service_unavailable",
                    "Persistence.DatabaseNotFound",
                    "Database trong chuỗi kết nối không tồn tại trên máy chủ PostgreSQL. Tạo database hoặc sửa tên trong ConnectionStrings.",
                    Array.Empty<string>()),

            PostgresErrorCodes.UndefinedTable
                or "42P01" => new ErrorDescriptor(
                    StatusCodes.Status503ServiceUnavailable,
                    "service_unavailable",
                    "Persistence.SchemaMissing",
                    "Schema database chưa khớp (thiếu bảng). Chạy migration: dotnet ef database update.",
                    Array.Empty<string>()),

            _ => new ErrorDescriptor(
                StatusCodes.Status503ServiceUnavailable,
                "service_unavailable",
                "Persistence.PostgresError",
                "Lỗi PostgreSQL khi thao tác dữ liệu. Kiểm tra log server và trạng thái database.",
                Array.Empty<string>())
        };

    private static ErrorDescriptor MapNpgsqlException(NpgsqlException npg)
    {
        if (npg.InnerException is PostgresException pg)
            return MapPostgresException(pg);

        return new ErrorDescriptor(
            StatusCodes.Status503ServiceUnavailable,
            "service_unavailable",
            "Persistence.DatabaseUnavailable",
            "Không kết nối được tới PostgreSQL (máy chủ không chạy, sai cổng/host, hoặc firewall). Kiểm tra dịch vụ Postgres và ConnectionStrings.",
            Array.Empty<string>());
    }

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
