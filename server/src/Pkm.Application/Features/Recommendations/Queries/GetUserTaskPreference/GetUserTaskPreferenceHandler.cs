using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Queries.GetUserTaskPreference;

public sealed class GetUserTaskPreferenceHandler : IQueryHandler<GetUserTaskPreferenceQuery, UserTaskPreferenceDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUserTaskPreferenceRepository _preferenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;

    public GetUserTaskPreferenceHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUserTaskPreferenceRepository preferenceRepository,
        IUnitOfWork unitOfWork,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _preferenceRepository = preferenceRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _clock = clock;
    }

    public async Task<Result<UserTaskPreferenceDto>> HandleAsync(
        GetUserTaskPreferenceQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<UserTaskPreferenceDto>(
                RecommendationErrors.InvalidPreferenceRequest(new[] { "WorkspaceId không hợp lệ." }));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<UserTaskPreferenceDto>(
                RecommendationErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<UserTaskPreferenceDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!access.CanRead)
            return Result.Failure<UserTaskPreferenceDto>(WorkspaceErrors.WorkspaceForbidden);

        var cacheKey = RecommendationCacheKeys.Preference(
            _cacheKeyFactory,
            request.WorkspaceId,
            currentUserId);

        var cached = await _cache.GetAsync<UserTaskPreferenceDto>(
            cacheKey,
            cancellationToken);

        if (cached is not null)
            return Result.Success(cached);

        var preference = await _preferenceRepository.GetByUserAndWorkspaceAsync(
            currentUserId,
            request.WorkspaceId,
            cancellationToken);

        if (preference is null)
        {
            preference = new UserTaskPreference(
                Guid.NewGuid(),
                currentUserId,
                request.WorkspaceId,
                _clock.UtcNow);

            _preferenceRepository.Add(preference);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dto = preference.ToDto();

        await _cache.SetAsync(
            cacheKey,
            dto,
            TimeSpan.FromMinutes(15),
            cancellationToken);

        return Result.Success(dto);
    }
}
