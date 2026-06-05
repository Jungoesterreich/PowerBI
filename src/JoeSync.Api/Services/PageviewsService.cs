// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Filters;
using JoeSync.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public interface IPageviewsService
{
    Task<IReadOnlyList<PageviewRowDto>> GetTopAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        string? searchTerm,
        string? searchPattern,
        int? top,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PageviewTimelinePointDto>> GetTimelineAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        string? searchTerm,
        string? searchPattern,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EditionAggregateDto>> GetByEditionAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? searchTerm,
        string? searchPattern,
        CancellationToken cancellationToken);
}

public sealed class PageviewsService : IPageviewsService
{
    private readonly JoeSyncDbContext _db;
    private readonly IEditionLookupService _editionLookup;

    public PageviewsService(JoeSyncDbContext db, IEditionLookupService editionLookup)
    {
        _db = db;
        _editionLookup = editionLookup;
    }

    public async Task<IReadOnlyList<PageviewRowDto>> GetTopAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        string? searchTerm,
        string? searchPattern,
        int? top,
        CancellationToken cancellationToken)
    {
        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe, searchTerm, FileType: null, SearchPattern: searchPattern);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var limit = Math.Clamp(top ?? 100, 1, 5000);

        var grouped = await _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "action")
            .ApplyCommonFilters(filter, editions)
            .GroupBy(a => new
            {
                a.Url,
                a.PageTitle,
                a.UrlPathFull,
                a.UrlPathElementOne,
                a.UrlPathElementTwo,
                a.Edition,
                a.SearchPatternIdu,
                a.SearchPatternTo,
                a.Seitenart,
                a.SubRubrik,
            })
            .Select(g => new
            {
                g.Key.Url,
                g.Key.PageTitle,
                g.Key.UrlPathFull,
                g.Key.UrlPathElementOne,
                g.Key.UrlPathElementTwo,
                g.Key.Edition,
                g.Key.SearchPatternIdu,
                g.Key.SearchPatternTo,
                g.Key.Seitenart,
                g.Key.SubRubrik,
                Hits = g.Count(),
                UniqueVisitors = g.Select(x => x.VisitorId).Distinct().Count(),
            })
            .OrderByDescending(r => r.Hits)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var meta = await _editionLookup.GetEditionMetaMapAsync(cancellationToken);

        return [.. grouped.Select(g => new PageviewRowDto(
            g.Url,
            g.PageTitle,
            g.UrlPathFull,
            g.UrlPathElementOne,
            g.UrlPathElementTwo,
            g.Edition,
            LookupSchuljahr(meta, g.Edition),
            g.SearchPatternIdu,
            g.SearchPatternTo,
            g.Seitenart,
            g.SubRubrik,
            g.Hits,
            g.UniqueVisitors))];
    }

    public async Task<IReadOnlyList<PageviewTimelinePointDto>> GetTimelineAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        string? searchTerm,
        string? searchPattern,
        CancellationToken cancellationToken)
    {
        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe, searchTerm, FileType: null, SearchPattern: searchPattern);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var baseQuery = _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "action")
            .ApplyCommonFilters(filter, editions)
            .Where(a => a.ServerDate != null);

        var hitsPerDay = await baseQuery
            .GroupBy(a => a.ServerDate!.Value)
            .Select(g => new { Date = g.Key, Hits = g.Count() })
            .ToListAsync(cancellationToken);

        var uniqueVisitorsPerDay = await baseQuery
            .Select(a => new { Date = a.ServerDate!.Value, a.VisitorId })
            .Distinct()
            .GroupBy(x => x.Date)
            .Select(g => new { Date = g.Key, UniqueVisitors = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.UniqueVisitors, cancellationToken);

        return [.. hitsPerDay
            .Select(h => new PageviewTimelinePointDto(
                h.Date,
                h.Hits,
                uniqueVisitorsPerDay.GetValueOrDefault(h.Date, 0)))
            .OrderBy(p => p.Date)];
    }

    public async Task<IReadOnlyList<EditionAggregateDto>> GetByEditionAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? searchTerm,
        string? searchPattern,
        CancellationToken cancellationToken)
    {
        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, Ausgabe: null, searchTerm, FileType: null, SearchPattern: searchPattern);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var grouped = await _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "action" && a.Edition != null)
            .ApplyCommonFilters(filter, editions)
            .GroupBy(a => a.Edition!)
            .Select(g => new
            {
                Edition = g.Key,
                Hits = g.Count(),
                UniqueVisitors = g.Select(x => x.VisitorId).Distinct().Count(),
            })
            .OrderByDescending(r => r.Hits)
            .ToListAsync(cancellationToken);

        var meta = await _editionLookup.GetEditionMetaMapAsync(cancellationToken);

        return [.. grouped.Select(g =>
        {
            meta.TryGetValue(g.Edition, out var m);
            return new EditionAggregateDto(
                g.Edition,
                m?.Schuljahr,
                m?.Jahr,
                m?.Ausgabe,
                g.Hits,
                g.UniqueVisitors);
        })];
    }

    private static string? LookupSchuljahr(
        IReadOnlyDictionary<string, EditionMeta> meta,
        string? edition) =>
        edition is not null && meta.TryGetValue(edition, out var m) ? m.Schuljahr : null;
}
