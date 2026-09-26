using achiev_hub.Server.Data;
using achiev_hub.Server.Enums;
using achiev_hub.Server.Options;
using achiev_hub.Server.Services;
using achiev_hub.Server.Services.Interfaces;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace achiev_hub.Server.Workers;

public class SyncWorkerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SyncWorkerOptions _options;
    private readonly ILogger<SyncWorkerBackgroundService> _logger;
    private readonly CronExpression _nightlyCron;
    private DateTimeOffset? _nextNightly;

    public SyncWorkerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SyncWorkerOptions> options,
        ILogger<SyncWorkerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
        _nightlyCron = CronExpression.Parse(_options.NightlyCron);
        _nextNightly = _nightlyCron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Sync worker started (concurrency={Concurrency}, nightly={Cron})",
            _options.Concurrency,
            _options.NightlyCron);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MaybeEnqueueNightlyAsync(stoppingToken);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<SyncJobProcessor>();
                var job = await processor.ClaimNextAsync(stoppingToken);

                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
                    continue;
                }

                _logger.LogInformation("Processing sync job {JobId} type {Type}", job.Id, job.Type);
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync worker loop error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task MaybeEnqueueNightlyAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (_nextNightly is null || now < _nextNightly)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var enqueue = scope.ServiceProvider.GetRequiredService<ISyncJobEnqueueService>();

        var alreadyPending = await db.SyncJobs.AnyAsync(
            j => j.Type == SyncJobType.MaintenanceNightly
                && (j.Status == SyncJobStatus.Pending || j.Status == SyncJobStatus.Running)
                && j.CreatedAt > now.AddHours(-12),
            cancellationToken);

        if (!alreadyPending)
        {
            await enqueue.EnqueueAsync(SyncJobType.MaintenanceNightly, null, null, cancellationToken: cancellationToken);
            _logger.LogInformation("Enqueued nightly maintenance sync job");
        }

        _nextNightly = _nightlyCron.GetNextOccurrence(now, TimeZoneInfo.Utc);
    }
}
