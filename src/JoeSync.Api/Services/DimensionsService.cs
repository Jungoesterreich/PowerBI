// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public interface IDimensionsService
{
    Task<IReadOnlyList<AusgabeDto>> GetAusgabenAsync(
        string? schuljahr,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetSchuljahreAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SiteDto>> GetSitesAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchTermDto>> GetSearchTermsAsync(
        int? siteId,
        int? top,
        CancellationToken cancellationToken);
}

public sealed class DimensionsService : IDimensionsService
{
    private readonly JoeSyncDbContext _db;

    public DimensionsService(JoeSyncDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AusgabeDto>> GetAusgabenAsync(
        string? schuljahr,
        CancellationToken cancellationToken)
    {
        var query = _db.DimAusgaben.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(schuljahr))
        {
            query = query.Where(d => d.Schuljahr == schuljahr);
        }

        return await query
            .OrderByDescending(d => d.Schuljahr)
            .ThenBy(d => d.Ausgabe)
            .Select(d => new AusgabeDto(
                d.Ausgabenkuerzel,
                d.Schuljahr,
                d.Jahr,
                d.Ausgabe,
                d.AusgabeIDU))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetSchuljahreAsync(
        CancellationToken cancellationToken)
    {
        return await _db.DimAusgaben
            .AsNoTracking()
            .Select(d => d.Schuljahr)
            .Distinct()
            .OrderByDescending(s => s)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SiteDto>> GetSitesAsync(
        CancellationToken cancellationToken)
    {
        return await _db.MatomoDailySummaries
            .AsNoTracking()
            .GroupBy(d => new { d.IdSite, d.SiteName })
            .Select(g => new SiteDto(g.Key.IdSite, g.Key.SiteName))
            .OrderBy(s => s.IdSite)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchTermDto>> GetSearchTermsAsync(
        int? siteId,
        int? top,
        CancellationToken cancellationToken)
    {
        var actions = _db.MatomoActionDetailsEnriched.AsNoTracking();

        if (siteId.HasValue)
        {
            actions = actions.Where(a => a.IdSite == siteId.Value);
        }

        // Empty strings count as "no pattern matched" — same semantics as PBI slicer.
        // Use anonymous projection so EF can translate the UNION; record constructors
        // are treated as client projections and break set-operation translation.
        var idu = actions
            .Where(a => a.SearchPatternIdu != null && a.SearchPatternIdu != string.Empty)
            .GroupBy(a => a.SearchPatternIdu!)
            .Select(g => new { Term = g.Key, Source = "IDU", Count = g.Count() });

        var to = actions
            .Where(a => a.SearchPatternTo != null && a.SearchPatternTo != string.Empty)
            .GroupBy(a => a.SearchPatternTo!)
            .Select(g => new { Term = g.Key, Source = "TO", Count = g.Count() });

        var limit = Math.Clamp(top ?? 200, 1, 1000);

        var rows = await idu
            .Concat(to)
            .OrderByDescending(t => t.Count)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return [.. rows.Select(r => new SearchTermDto(r.Term, r.Source, r.Count))];
    }
}
