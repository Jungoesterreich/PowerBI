// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Contracts;
using JoeSync.Core.Data;
using JoeSync.Core.Data.Entities;
using JoeSync.Core.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Sync;

public sealed class SyncRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncRunner> _logger;

    private int _running;
    private RunInfo? _current;

    public SyncRunner(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SyncRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsRunning => Volatile.Read(ref _running) != 0;

    public RunInfo? Current => _current;

    public IReadOnlyList<string> GetRegisteredJobNames()
    {
        using var scope = _scopeFactory.CreateScope();
        return [.. scope.ServiceProvider
            .GetServices<ISyncJob>()
            .Select(j => j.JobName)];
    }

    public async Task<RunHandle> StartJobAsync(
        string jobName,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var allJobs = GetRegisteredJobNames();
        var resolved = allJobs.FirstOrDefault(j =>
            string.Equals(j, jobName, StringComparison.OrdinalIgnoreCase));

        if (resolved is null)
        {
            throw new SyncJobNotFoundException(jobName);
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            throw new SyncBusyException(_current);
        }

        var syncLogId = await CreateLogEntryAsync(resolved, cancellationToken);

        var info = new RunInfo(resolved, syncLogId, DateTime.UtcNow, from, to);
        _current = info;

        _ = Task.Run(() => ExecuteSingleAsync(resolved, syncLogId, from, to));

        return new RunHandle([new RunHandleItem(resolved, syncLogId)]);
    }

    public async Task<RunHandle> StartAllJobsAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var jobs = GetRegisteredJobNames();
        if (jobs.Count == 0)
        {
            return new RunHandle([]);
        }

        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            throw new SyncBusyException(_current);
        }

        var items = new List<RunHandleItem>();
        foreach (var jobName in jobs)
        {
            var syncLogId = await CreateLogEntryAsync(
                jobName,
                cancellationToken,
                initialStatus: SyncLogStatus.Running,
                bumpStartTime: items.Count == 0);
            items.Add(new RunHandleItem(jobName, syncLogId));
        }

        _current = new RunInfo(
            jobs[0],
            items[0].SyncLogId,
            DateTime.UtcNow,
            from,
            to);

        _ = Task.Run(() => ExecuteAllAsync(items, from, to));

        return new RunHandle(items);
    }

    private async Task<int> CreateLogEntryAsync(
        string jobName,
        CancellationToken cancellationToken,
        SyncLogStatus initialStatus = SyncLogStatus.Running,
        bool bumpStartTime = true)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JoeSyncDbContext>();

        var log = new SyncLog
        {
            JobName = jobName,
            StartTime = bumpStartTime ? DateTime.UtcNow : DateTime.UtcNow,
            Status = initialStatus,
        };

        db.SyncLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return log.Id;
    }

    private async Task ExecuteSingleAsync(
        string jobName,
        int syncLogId,
        DateOnly? from,
        DateOnly? to)
    {
        try
        {
            ApplyOverrides(from, to);
            await RunJobInternalAsync(jobName, syncLogId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync run failed for {JobName}", jobName);
        }
        finally
        {
            ClearOverrides();
            _current = null;
            Interlocked.Exchange(ref _running, 0);
        }
    }

    private async Task ExecuteAllAsync(
        IReadOnlyList<RunHandleItem> items,
        DateOnly? from,
        DateOnly? to)
    {
        try
        {
            ApplyOverrides(from, to);

            foreach (var item in items)
            {
                _current = new RunInfo(
                    item.JobName,
                    item.SyncLogId,
                    DateTime.UtcNow,
                    from,
                    to);

                await RunJobInternalAsync(item.JobName, item.SyncLogId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Run-all failed");
        }
        finally
        {
            ClearOverrides();
            _current = null;
            Interlocked.Exchange(ref _running, 0);
        }
    }

    private async Task RunJobInternalAsync(string jobName, int syncLogId)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<JoeSyncDbContext>();
        var repo = sp.GetRequiredService<ISyncLogRepository>();

        var log = await db.SyncLogs.FirstOrDefaultAsync(l => l.Id == syncLogId);
        if (log is null)
        {
            _logger.LogError(
                "SyncLog #{Id} disappeared before {JobName} ran",
                syncLogId,
                jobName);
            return;
        }

        var job = sp.GetServices<ISyncJob>()
            .FirstOrDefault(j => string.Equals(
                j.JobName,
                jobName,
                StringComparison.OrdinalIgnoreCase));

        if (job is null)
        {
            await repo.LogFailureAsync(log, $"Job {jobName} not registered");
            return;
        }

        try
        {
            _logger.LogInformation("API-triggered sync starting: {JobName}", jobName);
            var result = await job.ExecuteAsync(CancellationToken.None);

            if (result.Success)
            {
                await repo.LogSuccessAsync(log, result.RowsAffected);
                _logger.LogInformation(
                    "API-triggered sync completed: {JobName} ({Rows} rows)",
                    jobName,
                    result.RowsAffected);
            }
            else
            {
                await repo.LogFailureAsync(log, result.ErrorMessage ?? "Unknown error");
                _logger.LogWarning(
                    "API-triggered sync failed: {JobName} — {Error}",
                    jobName,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await repo.LogFailureAsync(log, ex.Message);
            _logger.LogError(ex, "API-triggered sync threw: {JobName}", jobName);
        }
    }

    private void ApplyOverrides(DateOnly? from, DateOnly? to)
    {
        if (from is not null)
        {
            _configuration["Sync:OverrideStartDate"] = from.Value.ToString("yyyy-MM-dd");
        }

        if (to is not null)
        {
            _configuration["Sync:OverrideEndDate"] = to.Value.ToString("yyyy-MM-dd");
        }
    }

    private void ClearOverrides()
    {
        _configuration["Sync:OverrideStartDate"] = null;
        _configuration["Sync:OverrideEndDate"] = null;
    }
}

public sealed record RunInfo(
    string JobName,
    int SyncLogId,
    DateTime StartedAt,
    DateOnly? OverrideFrom,
    DateOnly? OverrideTo);

public sealed record RunHandle(IReadOnlyList<RunHandleItem> Items);

public sealed record RunHandleItem(string JobName, int SyncLogId);

public sealed class SyncBusyException : Exception
{
    public SyncBusyException(RunInfo? current)
        : base($"Sync already running: {current?.JobName ?? "unknown"}")
    {
        Current = current;
    }

    public RunInfo? Current { get; }
}

public sealed class SyncJobNotFoundException : Exception
{
    public SyncJobNotFoundException(string jobName)
        : base($"Sync job '{jobName}' not registered")
    {
        JobName = jobName;
    }

    public string JobName { get; }
}
