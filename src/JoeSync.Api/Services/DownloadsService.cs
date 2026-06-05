// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Filters;
using JoeSync.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public interface IDownloadsService
{
    Task<IReadOnlyList<DownloadRowDto>> GetTopAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        string? fileType,
        string? searchTerm,
        string? searchPattern,
        int? top,
        CancellationToken cancellationToken);
}

public sealed class DownloadsService : IDownloadsService
{
    private readonly JoeSyncDbContext _db;
    private readonly IEditionLookupService _editionLookup;

    public DownloadsService(JoeSyncDbContext db, IEditionLookupService editionLookup)
    {
        _db = db;
        _editionLookup = editionLookup;
    }

    public async Task<IReadOnlyList<DownloadRowDto>> GetTopAsync(
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        string? fileType,
        string? searchTerm,
        string? searchPattern,
        int? top,
        CancellationToken cancellationToken)
    {
        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe, searchTerm, fileType, searchPattern);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var limit = Math.Clamp(top ?? 100, 1, 5000);

        var grouped = await _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "download")
            .ApplyCommonFilters(filter, editions)
            .GroupBy(a => new
            {
                a.Url,
                a.UrlLastPathElement,
                a.FileType,
                a.Edition,
                a.SearchPatternIdu,
                a.SearchPatternTo,
            })
            .Select(g => new
            {
                g.Key.Url,
                g.Key.UrlLastPathElement,
                g.Key.FileType,
                g.Key.Edition,
                g.Key.SearchPatternIdu,
                g.Key.SearchPatternTo,
                Downloads = g.Count(),
                UniqueDownloads = g.Select(x => x.VisitorId).Distinct().Count(),
            })
            .OrderByDescending(r => r.Downloads)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var meta = await _editionLookup.GetEditionMetaMapAsync(cancellationToken);

        return [.. grouped.Select(g => new DownloadRowDto(
            g.Url,
            g.UrlLastPathElement,
            g.FileType,
            g.Edition,
            LookupSchuljahr(meta, g.Edition),
            g.SearchPatternIdu,
            g.SearchPatternTo,
            g.Downloads,
            g.UniqueDownloads))];
    }

    private static string? LookupSchuljahr(
        IReadOnlyDictionary<string, EditionMeta> meta,
        string? edition) =>
        edition is not null && meta.TryGetValue(edition, out var m) ? m.Schuljahr : null;
}
