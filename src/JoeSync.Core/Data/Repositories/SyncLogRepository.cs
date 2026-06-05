// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Core.Data.Repositories;

public sealed class SyncLogRepository : ISyncLogRepository
{
    private readonly JoeSyncDbContext _db;

    public SyncLogRepository(JoeSyncDbContext db)
    {
        _db = db;
    }

    public async Task<SyncLog> LogStartAsync(
        string jobName,
        CancellationToken cancellationToken = default)
    {
        var log = new SyncLog
        {
            JobName = jobName,
            StartTime = DateTime.UtcNow,
            Status = SyncLogStatus.Running,
        };

        _db.SyncLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task LogSuccessAsync(
        SyncLog log,
        int rowsAffected,
        CancellationToken cancellationToken = default)
    {
        log.EndTime = DateTime.UtcNow;
        log.Status = SyncLogStatus.Success;
        log.RowsAffected = rowsAffected;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task LogFailureAsync(
        SyncLog log,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        log.EndTime = DateTime.UtcNow;
        log.Status = SyncLogStatus.Failed;
        log.ErrorMessage = errorMessage;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DateTime?> GetLastSuccessfulRunAsync(
        string jobName,
        CancellationToken cancellationToken = default)
    {
        return await _db.SyncLogs
            .Where(l => l.JobName == jobName
                && l.Status == SyncLogStatus.Success)
            .OrderByDescending(l => l.StartTime)
            .Select(l => (DateTime?)l.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
