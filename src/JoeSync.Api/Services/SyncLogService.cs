// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Core.Data;
using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public interface ISyncLogService
{
    Task<IReadOnlyList<SyncLogDto>> GetRecentAsync(
        string? jobName,
        int? limit,
        CancellationToken cancellationToken);

    Task<SyncLogDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
}

public sealed class SyncLogService : ISyncLogService
{
    private readonly JoeSyncDbContext _db;

    public SyncLogService(JoeSyncDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SyncLogDto>> GetRecentAsync(
        string? jobName,
        int? limit,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit ?? 20, 1, 200);
        var query = _db.SyncLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(jobName))
        {
            query = query.Where(l => l.JobName == jobName);
        }

        return await query
            .OrderByDescending(l => l.StartTime)
            .Take(take)
            .Select(l => ToDto(l))
            .ToListAsync(cancellationToken);
    }

    public async Task<SyncLogDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.SyncLogs
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => ToDto(l))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static SyncLogDto ToDto(SyncLog l)
    {
        var duration = l.EndTime.HasValue
            ? (int?)(l.EndTime.Value - l.StartTime).TotalSeconds
            : null;

        return new SyncLogDto(
            l.Id,
            l.JobName,
            l.StartTime,
            l.EndTime,
            l.Status.ToString(),
            l.RowsAffected,
            l.ErrorMessage,
            duration);
    }
}
