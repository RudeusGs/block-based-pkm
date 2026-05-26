using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;

namespace Pkm.Application.Features.Pages.Services;

public sealed class PageTrashCleanupService : IPageTrashCleanupService
{
    private static readonly TimeSpan MinimumRetention = TimeSpan.FromDays(1);
    private static readonly TimeSpan MaximumRetention = TimeSpan.FromDays(3650);
    private const int DefaultBatchSize = 100;
    private const int MaximumBatchSize = 500;

    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PageTrashCleanupService(
        IPageWriteRepository pageWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _pageWriteRepository = pageWriteRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<int> CleanupExpiredArchivedPagesAsync(
        TimeSpan retention,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        retention = NormalizeRetention(retention);
        batchSize = NormalizeBatchSize(batchSize);

        var now = _clock.UtcNow;
        var archiveCutoffUtc = now.Subtract(retention);

        var deletedPageCount = await _pageWriteRepository.SoftDeleteExpiredArchivedPagesAsync(
            archiveCutoffUtc,
            now,
            batchSize,
            cancellationToken);

        if (deletedPageCount <= 0)
            return 0;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return deletedPageCount;
    }

    private static TimeSpan NormalizeRetention(TimeSpan retention)
    {
        if (retention < MinimumRetention)
            return MinimumRetention;

        if (retention > MaximumRetention)
            return MaximumRetention;

        return retention;
    }

    private static int NormalizeBatchSize(int batchSize)
    {
        if (batchSize <= 0)
            return DefaultBatchSize;

        return Math.Min(batchSize, MaximumBatchSize);
    }
}
