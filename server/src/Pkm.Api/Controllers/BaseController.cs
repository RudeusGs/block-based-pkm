using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Common;
using Pkm.Api.Contracts.Common;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Common.Results;

namespace Pkm.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    protected BaseController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    protected ActionResult<ApiResult> HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return StatusCode(
                StatusCodes.Status200OK,
                ApiResult.Success(
                    statusCode: StatusCodes.Status200OK,
                    traceId: HttpContext.TraceIdentifier));
        }

        var response = ApiErrorResponseFactory.FromApplicationError(
            result.Error,
            HttpContext.TraceIdentifier);

        return StatusCode(response.StatusCode, response);
    }

    protected ActionResult<ApiResult<T>> HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return StatusCode(
                StatusCodes.Status200OK,
                ApiResult<T>.Success(
                    result.Value,
                    statusCode: StatusCodes.Status200OK,
                    traceId: HttpContext.TraceIdentifier));
        }

        var errorResponse = ApiErrorResponseFactory.FromApplicationError(
            result.Error,
            HttpContext.TraceIdentifier);

        var response = ApiResult<T>.Failure(
            message: errorResponse.Message,
            error: errorResponse.Error!,
            statusCode: errorResponse.StatusCode,
            traceId: errorResponse.TraceId);

        return StatusCode(response.StatusCode, response);
    }

    protected ActionResult<ApiResult<TOut>> HandleResult<TIn, TOut>(
        Result<TIn> result,
        Func<TIn, TOut> mapper)
    {
        if (result.IsSuccess)
        {
            return StatusCode(
                StatusCodes.Status200OK,
                ApiResult<TOut>.Success(
                    mapper(result.Value),
                    statusCode: StatusCodes.Status200OK,
                    traceId: HttpContext.TraceIdentifier));
        }

        var errorResponse = ApiErrorResponseFactory.FromApplicationError(
            result.Error,
            HttpContext.TraceIdentifier);

        var response = ApiResult<TOut>.Failure(
            message: errorResponse.Message,
            error: errorResponse.Error!,
            statusCode: errorResponse.StatusCode,
            traceId: errorResponse.TraceId);

        return StatusCode(response.StatusCode, response);
    }

    protected bool TryGetCurrentUserId(out Guid userId)
        => _currentUser.TryGetUserId(out userId);

    protected Guid? CurrentUserId => _currentUser.UserId;

    protected bool IsAuthenticated => _currentUser.IsAuthenticated;
}