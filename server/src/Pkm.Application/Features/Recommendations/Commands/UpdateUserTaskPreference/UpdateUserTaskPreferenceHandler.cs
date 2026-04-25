using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Common;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Commands.UpdateUserTaskPreference;

public sealed class UpdateUserTaskPreferenceHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUserTaskPreferenceRepository _preferenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IClock _clock;
    private readonly UpdateUserTaskPreferenceCommandValidator _validator;

    public UpdateUserTaskPreferenceHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUserTaskPreferenceRepository preferenceRepository,
        IUnitOfWork unitOfWork,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IClock clock,
        UpdateUserTaskPreferenceCommandValidator validator)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _preferenceRepository = preferenceRepository;
        _unitOfWork = unitOfWork;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<UserTaskPreferenceDto>> HandleAsync(
        UpdateUserTaskPreferenceCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<UserTaskPreferenceDto>(
                RecommendationErrors.InvalidPreferenceRequest(validationErrors));
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

        try
        {
            var now = _clock.UtcNow;

            var preference = await _preferenceRepository.GetByUserAndWorkspaceForUpdateAsync(
                currentUserId,
                request.WorkspaceId,
                cancellationToken);

            if (preference is null)
            {
                preference = new UserTaskPreference(
                    Guid.NewGuid(),
                    currentUserId,
                    request.WorkspaceId,
                    now);

                _preferenceRepository.Add(preference);
            }

            preference.UpdateWorkHours(request.WorkDayStartHour, request.WorkDayEndHour, now);
            preference.SetPreferredDays(request.PreferredDaysOfWeek, now);
            preference.UpdateMaxRecommendations(request.MaxRecommendationsPerSession, now);
            preference.UpdateMinPriority(request.MinPriorityForRecommendation, now);
            preference.UpdateSensitivity(request.RecommendationSensitivity, now);
            preference.UpdateRecommendationInterval(request.RecommendationIntervalMinutes, now);
            preference.SetAutoRecommendation(request.EnableAutoRecommendation, now);

            _preferenceRepository.Update(preference);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = preference.ToDto();

            await _redisCache.SetAsync(
                RecommendationCacheKeys.Preference(_redisKeyFactory, request.WorkspaceId, currentUserId),
                dto,
                TimeSpan.FromMinutes(15),
                cancellationToken);

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<UserTaskPreferenceDto>(
                RecommendationErrors.OperationFailed(ex.Message));
        }
    }
}