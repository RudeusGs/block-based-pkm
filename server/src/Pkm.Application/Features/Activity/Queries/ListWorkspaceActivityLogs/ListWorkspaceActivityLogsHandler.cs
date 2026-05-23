using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Activity.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Audit;

namespace Pkm.Application.Features.Activity.Queries.ListWorkspaceActivityLogs;

public sealed class ListWorkspaceActivityLogsHandler
{
    private const int MaxPageSize = 100;

    private readonly ICurrentUser _currentUser;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;

    public ListWorkspaceActivityLogsHandler(
        ICurrentUser currentUser,
        IActivityLogRepository activityLogRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator)
    {
        _currentUser = currentUser;
        _activityLogRepository = activityLogRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
    }

    public async Task<Result<ActivityLogPagedResultDto>> HandleAsync(
        ListWorkspaceActivityLogsQuery request,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<ActivityLogPagedResultDto>(
                ActivityErrors.InvalidListRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<ActivityLogPagedResultDto>(ActivityErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<ActivityLogPagedResultDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanReadAudit)
        {
            return Result.Failure<ActivityLogPagedResultDto>(ActivityErrors.ActivityForbidden);
        }

        ActivityAction? action = null;
        ActivityEntityType? entityType = null;

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            action = ParseActivityAction(request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            entityType = ParseActivityEntityType(request.EntityType);
        }

        var safePageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var safePageSize = request.PageSize <= 0 ? 30 : Math.Min(request.PageSize, MaxPageSize);

        var items = await _activityLogRepository.ListByWorkspaceAsync(
            request.WorkspaceId,
            action,
            entityType,
            NormalizeUserId(request.UserId),
            request.FromUtc,
            request.ToUtc,
            NormalizeSearch(request.Search),
            safePageNumber,
            safePageSize,
            cancellationToken);

        var totalCount = await _activityLogRepository.CountByWorkspaceAsync(
            request.WorkspaceId,
            action,
            entityType,
            NormalizeUserId(request.UserId),
            request.FromUtc,
            request.ToUtc,
            NormalizeSearch(request.Search),
            cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)safePageSize);

        return Result.Success(new ActivityLogPagedResultDto(
            items,
            safePageNumber,
            safePageSize,
            totalCount,
            totalPages));
    }

    private static List<string> Validate(ListWorkspaceActivityLogsQuery request)
    {
        var errors = new List<string>();

        if (request.WorkspaceId == Guid.Empty)
        {
            errors.Add("WorkspaceId không hợp lệ.");
        }

        if (request.PageNumber <= 0)
        {
            errors.Add("PageNumber phải lớn hơn 0.");
        }

        if (request.PageSize <= 0 || request.PageSize > MaxPageSize)
        {
            errors.Add($"PageSize phải nằm trong khoảng 1 - {MaxPageSize}.");
        }

        if (request.UserId == Guid.Empty)
        {
            errors.Add("UserId không hợp lệ.");
        }

        if (request.FromUtc.HasValue &&
            request.ToUtc.HasValue &&
            request.FromUtc.Value > request.ToUtc.Value)
        {
            errors.Add("FromUtc phải nhỏ hơn hoặc bằng ToUtc.");
        }

        if (!string.IsNullOrWhiteSpace(request.Search) &&
            request.Search.Trim().Length > 100)
        {
            errors.Add("Từ khóa tìm kiếm tối đa 100 ký tự.");
        }

        if (!string.IsNullOrWhiteSpace(request.Action) && ParseActivityAction(request.Action) is null)
        {
            errors.Add("Action không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType) && ParseActivityEntityType(request.EntityType) is null)
        {
            errors.Add("EntityType không hợp lệ.");
        }

        return errors;
    }

    private static ActivityAction? ParseActivityAction(string raw)
    {
        return Enum.TryParse<ActivityAction>(raw.Trim(), ignoreCase: true, out var action)
            ? action
            : null;
    }

    private static ActivityEntityType? ParseActivityEntityType(string raw)
    {
        return Enum.TryParse<ActivityEntityType>(raw.Trim(), ignoreCase: true, out var entityType)
            ? entityType
            : null;
    }

    private static Guid? NormalizeUserId(Guid? userId)
        => userId.HasValue && userId.Value != Guid.Empty
            ? userId
            : null;

    private static string? NormalizeSearch(string? search)
    {
        var value = search?.Trim();

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }
}
