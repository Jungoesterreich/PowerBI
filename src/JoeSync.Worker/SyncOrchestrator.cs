// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Contracts;
using JoeSync.Core.Data.Repositories;

namespace JoeSync.Worker;

public sealed class SyncOrchestrator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncOrchestrator> _logger;
    private readonly IConfiguration _configuration;

    public SyncOrchestrator(
        IServiceScopeFactory scopeFactory,
        ILogger<SyncOrchestrator> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var triggerTimeStr = _configuration["Sync:TriggerTime"] ?? "21:00";
        var triggerTime = TimeOnly.Parse(triggerTimeStr);

        _logger.LogInformation(
            "SyncOrchestrator started. Next trigger at {TriggerTime}",
            triggerTime);

        while (!cancellationToken.IsCancellationRequested)
        {
            var delay = CalculateDelay(triggerTime, DateTime.Now);
            _logger.LogInformation(
                "Waiting {Delay} until next sync at {TriggerTime}",
                delay,
                triggerTime);

            await Task.Delay(delay, cancellationToken);

            await RunAllJobsAsync(cancellationToken);
        }
    }

    private async Task RunAllJobsAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider
            .GetServices<ISyncJob>().ToList();

        if (jobs.Count == 0)
        {
            _logger.LogWarning("No sync jobs registered");
            return;
        }

        _logger.LogInformation(
            "Starting sync run with {JobCount} job(s)", jobs.Count);

        foreach (var job in jobs)
        {
            await RunSingleJobAsync(scope, job, cancellationToken);
        }

        _logger.LogInformation("Sync run completed");
    }

    private async Task RunSingleJobAsync(
        IServiceScope scope,
        ISyncJob job,
        CancellationToken cancellationToken = default)
    {
        var syncLogRepo = scope.ServiceProvider
            .GetRequiredService<ISyncLogRepository>();

        var log = await syncLogRepo.LogStartAsync(
            job.JobName, cancellationToken);

        try
        {
            _logger.LogInformation(
                "Executing job {JobName}", job.JobName);

            var result = await job.ExecuteAsync(cancellationToken);

            if (result.Success)
            {
                await syncLogRepo.LogSuccessAsync(
                    log, result.RowsAffected, cancellationToken);

                _logger.LogInformation(
                    "Job {JobName} completed successfully. {RowsAffected} rows",
                    job.JobName,
                    result.RowsAffected);
            }
            else
            {
                await syncLogRepo.LogFailureAsync(
                    log,
                    result.ErrorMessage ?? "Unknown error",
                    cancellationToken);

                _logger.LogError(
                    "Job {JobName} failed: {ErrorMessage}",
                    job.JobName,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await syncLogRepo.LogFailureAsync(
                log, ex.Message, cancellationToken);

            _logger.LogError(
                ex,
                "Job {JobName} threw an exception",
                job.JobName);
        }
    }

    internal static TimeSpan CalculateDelay(TimeOnly triggerTime, DateTime now)
    {
        var todayTrigger = now.Date.Add(triggerTime.ToTimeSpan());

        var nextTrigger = now < todayTrigger
            ? todayTrigger
            : todayTrigger.AddDays(1);

        return nextTrigger - now;
    }
}
