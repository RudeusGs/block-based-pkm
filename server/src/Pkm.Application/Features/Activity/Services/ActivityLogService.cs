using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Domain.Audit;
using Pkm.Domain.Common;

namespace Pkm.Application.Features.Activity.Services;

public sealed class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivityLogService(
        IActivityLogRepository activityLogRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _activityLogRepository = activityLogRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task RecordAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.WorkspaceId == Guid.Empty ||
                request.UserId == Guid.Empty ||
                request.EntityId == Guid.Empty)
            {
                return;
            }

            var log = ActivityLog.Create(
                request.WorkspaceId,
                request.UserId,
                request.Action,
                request.EntityType,
                request.EntityId,
                _clock.UtcNow,
                request.Description,
                request.MetadataJson,
                request.IpAddress);

            _activityLogRepository.Add(log);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DomainException)
        {
            // Activity log must never break the main business flow.
        }
        catch
        {
            // Best-effort audit trail for the UI panel.
        }
    }
}
