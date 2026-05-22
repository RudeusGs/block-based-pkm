namespace Pkm.Application.Features.Activity.Services;

public interface IActivityLogService
{
    Task RecordAsync(
        ActivityLogRequest request,
        CancellationToken cancellationToken = default);
}
