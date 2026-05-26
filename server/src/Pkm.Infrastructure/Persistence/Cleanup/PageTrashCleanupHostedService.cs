using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pkm.Application.Features.Pages.Services;

namespace Pkm.Infrastructure.Persistence.Cleanup;

internal sealed class PageTrashCleanupHostedService : BackgroundService
{
    private const int MinimumRetentionDays = 1;
    private const int MaximumRetentionDays = 3650;
    private const int DefaultIntervalMinutes = 60;
    private const int MinimumIntervalMinutes = 5;
    private const int DefaultBatchSize = 100;
    private const int MaximumBatchSize = 500;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<PageTrashCleanupOptions> _options;
    private readonly ILogger<PageTrashCleanupHostedService> _logger;

    public PageTrashCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<PageTrashCleanupOptions> options,
        ILogger<PageTrashCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var options = _options.CurrentValue;

                if (options.Enabled)
                    await CleanupOnceAsync(options, stoppingToken);

                await Task.Delay(GetInterval(options), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Page Trash cleanup failed.");
                await DelayAfterFailureAsync(stoppingToken);
            }
        }
    }

    private async Task CleanupOnceAsync(
        PageTrashCleanupOptions options,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IPageTrashCleanupService>();

        var retention = TimeSpan.FromDays(NormalizeRetentionDays(options.RetentionDays));
        var batchSize = NormalizeBatchSize(options.BatchSize);

        var deletedPageCount = await cleanupService.CleanupExpiredArchivedPagesAsync(
            retention,
            batchSize,
            cancellationToken);

        if (deletedPageCount > 0)
        {
            _logger.LogInformation(
                "Page Trash cleanup removed {DeletedPageCount} expired page(s). RetentionDays={RetentionDays}, BatchSize={BatchSize}.",
                deletedPageCount,
                retention.TotalDays,
                batchSize);
        }
    }

    private static int NormalizeRetentionDays(int retentionDays)
        => Math.Clamp(retentionDays, MinimumRetentionDays, MaximumRetentionDays);

    private static int NormalizeBatchSize(int batchSize)
    {
        if (batchSize <= 0)
            return DefaultBatchSize;

        return Math.Min(batchSize, MaximumBatchSize);
    }

    private static TimeSpan GetInterval(PageTrashCleanupOptions options)
    {
        var intervalMinutes = options.IntervalMinutes <= 0
            ? DefaultIntervalMinutes
            : Math.Max(options.IntervalMinutes, MinimumIntervalMinutes);

        return TimeSpan.FromMinutes(intervalMinutes);
    }

    private static async Task DelayAfterFailureAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(DefaultIntervalMinutes), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
