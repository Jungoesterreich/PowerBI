// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Core.Data;
using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public interface IDailyKpiService
{
    Task<IReadOnlyList<DailyKpiDto>> GetTimeseriesAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken);

    Task<DailySummaryDto> GetSummaryAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken);
}

public sealed class DailyKpiService : IDailyKpiService
{
    private readonly JoeSyncDbContext _db;

    public DailyKpiService(JoeSyncDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DailyKpiDto>> GetTimeseriesAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_db.MatomoDailySummaries.AsNoTracking(), siteId, from, to);

        // Project the raw columns in SQL, then compute the bounce rate in
        // memory via the shared (unit-tested) helper.
        var rows = await query
            .OrderBy(d => d.Date)
            .Select(d => new
            {
                d.Date,
                d.SiteName,
                d.Visits,
                d.UniqueVisitors,
                d.PageViews,
                d.UniquePageViews,
                d.Downloads,
                d.UniqueDownloads,
                d.Events,
                d.Searches,
                d.AvgVisitDuration,
                d.BounceCount,
            })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(d => new DailyKpiDto(
            d.Date,
            d.SiteName,
            d.Visits,
            d.UniqueVisitors,
            d.PageViews,
            d.UniquePageViews,
            d.Downloads,
            d.UniqueDownloads,
            d.Events,
            d.Searches,
            d.AvgVisitDuration,
            d.BounceCount,
            Metrics.BounceRate(d.BounceCount, d.Visits)))];
    }

    public async Task<DailySummaryDto> GetSummaryAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(_db.MatomoDailySummaries.AsNoTracking(), siteId, from, to);

        var totals = await query
            .GroupBy(d => 1)
            .Select(g => new
            {
                Visits = g.Sum(x => x.Visits),
                UniqueVisitors = g.Sum(x => x.UniqueVisitors),
                PageViews = g.Sum(x => x.PageViews),
                Downloads = g.Sum(x => x.Downloads),
                Events = g.Sum(x => x.Events),
                Searches = g.Sum(x => x.Searches),
                BounceCount = g.Sum(x => x.BounceCount),
                AvgVisitDuration = (int)g.Average(x => (double)x.AvgVisitDuration),
                DaysWithData = g.Count(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (totals is null)
        {
            return new DailySummaryDto(from, to, 0, 0, 0, 0, 0, 0, 0, 0m, 0, 0);
        }

        var bounceRate = Metrics.BounceRate(totals.BounceCount, totals.Visits);

        return new DailySummaryDto(
            from,
            to,
            totals.Visits,
            totals.UniqueVisitors,
            totals.PageViews,
            totals.Downloads,
            totals.Events,
            totals.Searches,
            totals.BounceCount,
            bounceRate,
            totals.AvgVisitDuration,
            totals.DaysWithData);
    }

    private static IQueryable<MatomoDailySummary> ApplyFilters(
        IQueryable<MatomoDailySummary> source,
        int? siteId,
        DateTime? from,
        DateTime? to)
    {
        var query = source;

        if (siteId.HasValue)
        {
            query = query.Where(d => d.IdSite == siteId.Value);
        }

        if (from.HasValue)
        {
            var fromDate = from.Value.Date;
            query = query.Where(d => d.Date >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.Date;
            query = query.Where(d => d.Date <= toDate);
        }

        return query;
    }
}
