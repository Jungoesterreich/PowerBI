// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Services;

public sealed record EditionMeta(string Schuljahr, int? Jahr, int? Ausgabe);

public interface IEditionLookupService
{
    Task<IReadOnlyCollection<string>?> ResolveSchuljahrEditionsAsync(
        string? schuljahr,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, EditionMeta>> GetEditionMetaMapAsync(
        CancellationToken cancellationToken);
}

public sealed class EditionLookupService : IEditionLookupService
{
    private readonly JoeSyncDbContext _db;

    public EditionLookupService(JoeSyncDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<string>?> ResolveSchuljahrEditionsAsync(
        string? schuljahr,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(schuljahr))
        {
            return null;
        }

        return await _db.DimAusgaben
            .AsNoTracking()
            .Where(d => d.Schuljahr == schuljahr)
            .Select(d => d.Ausgabenkuerzel)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, EditionMeta>> GetEditionMetaMapAsync(
        CancellationToken cancellationToken)
    {
        var rows = await _db.DimAusgaben
            .AsNoTracking()
            .Select(d => new { d.Ausgabenkuerzel, d.Schuljahr, d.Jahr, d.Ausgabe })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.Ausgabenkuerzel,
            r => new EditionMeta(r.Schuljahr, r.Jahr, r.Ausgabe),
            StringComparer.Ordinal);
    }
}
