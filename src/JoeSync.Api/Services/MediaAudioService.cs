// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public interface IMediaAudioService
{
    Task<IReadOnlyList<MediaAudioRowDto>> GetTopAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        int? top,
        CancellationToken cancellationToken);
}

public sealed class MediaAudioService : IMediaAudioService
{
    private readonly JoeSyncDbContext _db;
    private readonly IEditionLookupService _editionLookup;

    public MediaAudioService(JoeSyncDbContext db, IEditionLookupService editionLookup)
    {
        _db = db;
        _editionLookup = editionLookup;
    }

    public async Task<IReadOnlyList<MediaAudioRowDto>> GetTopAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        int? top,
        CancellationToken cancellationToken)
    {
        var query = _db.MatomoMediaAudioDailies.AsNoTracking();

        if (siteId.HasValue)
        {
            query = query.Where(m => m.IdSite == siteId.Value);
        }

        if (from.HasValue)
        {
            var fromDate = from.Value.Date;
            query = query.Where(m => m.Date >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.Date;
            query = query.Where(m => m.Date <= toDate);
        }

        if (!string.IsNullOrWhiteSpace(ausgabe))
        {
            query = query.Where(m => m.Edition == ausgabe);
        }
        else
        {
            var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);
            if (editions is { Count: > 0 })
            {
                query = query.Where(m => m.Edition != null && editions.Contains(m.Edition));
            }
        }

        var limit = Math.Clamp(top ?? 100, 1, 5000);

        var grouped = await query
            .GroupBy(m => new
            {
                m.Label,
                m.Url,
                m.UrlLastPathElement,
                m.Edition,
            })
            .Select(g => new
            {
                g.Key.Label,
                g.Key.Url,
                g.Key.UrlLastPathElement,
                g.Key.Edition,
                Plays = g.Sum(x => x.Plays),
                UniqueVisitorsPlays = g.Sum(x => x.UniqueVisitorsPlays),
                Finishes = g.Sum(x => x.Finishes),
                AvgTimeWatched = g.Average(x => (double)x.AvgTimeWatched),
            })
            .OrderByDescending(r => r.Plays)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var meta = await _editionLookup.GetEditionMetaMapAsync(cancellationToken);

        return [.. grouped.Select(g => new MediaAudioRowDto(
            g.Label,
            g.Url,
            g.UrlLastPathElement,
            g.Edition,
            g.Edition is not null && meta.TryGetValue(g.Edition, out var m) ? m.Schuljahr : null,
            g.Plays,
            g.UniqueVisitorsPlays,
            g.Finishes,
            Metrics.FinishRate(g.Finishes, g.Plays),
            g.Plays == 0 ? 0 : (int)g.AvgTimeWatched))];
    }
}
