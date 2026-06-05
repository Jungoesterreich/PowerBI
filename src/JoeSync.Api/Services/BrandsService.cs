// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Brands;
using JoeSync.Api.Dtos;
using JoeSync.Api.Filters;
using JoeSync.Core.Data;
using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public interface IBrandsService
{
    Task<BrandSummaryDto?> GetSummaryAsync(
        string key,
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PageviewRowDto>?> GetPageviewsAsync(
        string key,
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        string? searchTerm,
        string? searchPattern,
        int? top,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DownloadRowDto>?> GetDownloadsAsync(
        string key,
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

    Task<IReadOnlyList<BrandTimelinePointDto>?> GetTimelineAsync(
        string key,
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        CancellationToken cancellationToken);
}

public sealed class BrandsService : IBrandsService
{
    private readonly JoeSyncDbContext _db;
    private readonly IEditionLookupService _editionLookup;

    public BrandsService(JoeSyncDbContext db, IEditionLookupService editionLookup)
    {
        _db = db;
        _editionLookup = editionLookup;
    }

    public async Task<BrandSummaryDto?> GetSummaryAsync(
        string key,
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        CancellationToken cancellationToken)
    {
        var brand = JoeBrands.Find(key);
        if (brand is null)
        {
            return null;
        }

        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe,
            SearchTerm: null, FileType: null);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var pageviewQ = _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "action")
            .ApplyCommonFilters(filter, editions)
            .ApplyBrandFilter(brand);

        var downloadQ = _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "download")
            .ApplyCommonFilters(filter, editions)
            .ApplyBrandFilter(brand);

        var pvCount = await pageviewQ.CountAsync(cancellationToken);
        var pvUnique = await pageviewQ
            .Select(a => a.VisitorId)
            .Distinct()
            .CountAsync(cancellationToken);
        var pvPages = await pageviewQ
            .Select(a => a.UrlClean)
            .Distinct()
            .CountAsync(cancellationToken);

        var dlCount = await downloadQ.CountAsync(cancellationToken);
        var dlUnique = await downloadQ
            .Select(a => a.VisitorId)
            .Distinct()
            .CountAsync(cancellationToken);
        var dlFiles = await downloadQ
            .Select(a => a.Url)
            .Distinct()
            .CountAsync(cancellationToken);

        return new BrandSummaryDto(
            brand.Key,
            brand.Label,
            pvCount,
            pvUnique,
            dlCount,
            dlUnique,
            pvPages,
            dlFiles);
    }

    public async Task<IReadOnlyList<PageviewRowDto>?> GetPageviewsAsync(
        string key,
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
        var brand = JoeBrands.Find(key);
        if (brand is null)
        {
            return null;
        }

        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe, searchTerm, FileType: null, SearchPattern: searchPattern);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var limit = Math.Clamp(top ?? 100, 1, 5000);

        var grouped = await _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "action")
            .ApplyCommonFilters(filter, editions)
            .ApplyBrandFilter(brand)
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

    public async Task<IReadOnlyList<DownloadRowDto>?> GetDownloadsAsync(
        string key,
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
        var brand = JoeBrands.Find(key);
        if (brand is null)
        {
            return null;
        }

        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe, searchTerm, fileType, SearchPattern: searchPattern);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var limit = Math.Clamp(top ?? 100, 1, 5000);

        var grouped = await _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .Where(a => a.Type == "download")
            .ApplyCommonFilters(filter, editions)
            .ApplyBrandFilter(brand)
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

    public async Task<IReadOnlyList<BrandTimelinePointDto>?> GetTimelineAsync(
        string key,
        int? siteId,
        DateTime? from,
        DateTime? to,
        string? schuljahr,
        string? ausgabe,
        CancellationToken cancellationToken)
    {
        var brand = JoeBrands.Find(key);
        if (brand is null)
        {
            return null;
        }

        var filter = new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe,
            SearchTerm: null, FileType: null);
        var editions = await _editionLookup.ResolveSchuljahrEditionsAsync(schuljahr, cancellationToken);

        var perTypeCounts = await _db.MatomoActionDetailsEnriched
            .AsNoTracking()
            .ApplyCommonFilters(filter, editions)
            .ApplyBrandFilter(brand)
            .Where(a => a.ServerDate != null && a.Type != null)
            .GroupBy(a => new { Date = a.ServerDate!.Value, a.Type })
            .Select(g => new
            {
                g.Key.Date,
                g.Key.Type,
                Count = g.Count(),
            })
            .ToListAsync(cancellationToken);

        return [.. perTypeCounts
            .GroupBy(x => x.Date)
            .Select(g => new BrandTimelinePointDto(
                g.Key,
                g.Where(x => x.Type == "action").Sum(x => x.Count),
                g.Where(x => x.Type == "download").Sum(x => x.Count)))
            .OrderBy(p => p.Date)];
    }

    private static string? LookupSchuljahr(
        IReadOnlyDictionary<string, EditionMeta> meta,
        string? edition) =>
        edition is not null && meta.TryGetValue(edition, out var m) ? m.Schuljahr : null;
}

internal static class BrandQueryExtensions
{
    internal static IQueryable<MatomoActionDetailEnriched> ApplyBrandFilter(
        this IQueryable<MatomoActionDetailEnriched> source,
        BrandDefinition brand)
    {
        var contains = $"%{brand.UrlContains}%";
        var query = source.Where(a =>
            a.Url != null && EF.Functions.Like(a.Url, contains));

        if (!string.IsNullOrEmpty(brand.UrlExcludes))
        {
            var excludes = $"%{brand.UrlExcludes}%";
            query = query.Where(a =>
                a.Url == null || !EF.Functions.Like(a.Url, excludes));
        }

        return query;
    }
}
