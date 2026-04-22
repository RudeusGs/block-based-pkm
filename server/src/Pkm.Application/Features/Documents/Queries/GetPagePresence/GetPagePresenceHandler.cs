using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Documents.Queries.GetPagePresence;

public sealed class GetPagePresenceHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPagePresenceService _pagePresenceService;

    public GetPagePresenceHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IPagePresenceService pagePresenceService)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pagePresenceService = pagePresenceService;
    }

    public async Task<Result<PagePresenceDto>> HandleAsync(
        GetPagePresenceQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PagePresenceDto>(DocumentErrors.InvalidPageId);

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PagePresenceDto>(DocumentErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<PagePresenceDto>(DocumentErrors.PageNotFound);

        if (!access.CanReadDocument)
            return Result.Failure<PagePresenceDto>(DocumentErrors.PageForbidden);

        var entries = await _pagePresenceService.GetActiveOnPageAsync(
            request.PageId,
            cancellationToken);

        var users = entries
            .GroupBy(x => new { x.UserId, x.UserName })
            .Select(group =>
            {
                var lastSeenUtc = group.Max(x => x.LastSeenUtc);
                return new PagePresenceUserDto(
                    group.Key.UserId,
                    group.Key.UserName,
                    group.Count(),
                    lastSeenUtc);
            })
            .OrderByDescending(x => x.LastSeenUtc)
            .ToArray();

        return Result.Success(new PagePresenceDto(
            access.WorkspaceId,
            request.PageId,
            users));
    }
}