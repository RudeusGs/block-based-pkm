namespace Pkm.Api.Contracts.Common;

public record ApiError(
    string Code,
    string Type,
    IReadOnlyList<string> Details);

public record ApiResult(
    bool IsSuccess,
    string? Message,
    ApiError? Error,
    int StatusCode,
    string? TraceId)
{
    public static ApiResult Success(
        string? message = null,
        int statusCode = StatusCodes.Status200OK,
        string? traceId = null)
        => new(
            IsSuccess: true,
            Message: message,
            Error: null,
            StatusCode: statusCode,
            TraceId: traceId);

    public static ApiResult Failure(
        string? message,
        ApiError error,
        int statusCode,
        string? traceId = null)
        => new(
            IsSuccess: false,
            Message: message,
            Error: error,
            StatusCode: statusCode,
            TraceId: traceId);
}

public record ApiResult<T>(
    bool IsSuccess,
    string? Message,
    T? Data,
    ApiError? Error,
    int StatusCode,
    string? TraceId)
{
    public static ApiResult<T> Success(
        T data,
        string? message = null,
        int statusCode = StatusCodes.Status200OK,
        string? traceId = null)
        => new(
            IsSuccess: true,
            Message: message,
            Data: data,
            Error: null,
            StatusCode: statusCode,
            TraceId: traceId);

    public static ApiResult<T> Failure(
        string? message,
        ApiError error,
        int statusCode,
        string? traceId = null)
        => new(
            IsSuccess: false,
            Message: message,
            Data: default,
            Error: error,
            StatusCode: statusCode,
            TraceId: traceId);
}