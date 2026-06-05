// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Api.Filters;

public sealed record MatomoQueryFilter(
    int? SiteId,
    DateTime? From,
    DateTime? To,
    string? Schuljahr,
    string? Ausgabe,
    string? SearchTerm,
    string? FileType,
    string? SearchPattern = null);

public static class MatomoFilterExtensions
{
    public static IQueryable<MatomoActionDetailEnriched> ApplyCommonFilters(
        this IQueryable<MatomoActionDetailEnriched> source,
        MatomoQueryFilter filter,
        IReadOnlyCollection<string>? schuljahrEditions)
    {
        var query = source;

        if (filter.SiteId.HasValue)
        {
            query = query.Where(a => a.IdSite == filter.SiteId.Value);
        }

        if (filter.From.HasValue)
        {
            var from = filter.From.Value.Date;
            query = query.Where(a => a.ServerDate != null && a.ServerDate >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.Date;
            query = query.Where(a => a.ServerDate != null && a.ServerDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.Ausgabe))
        {
            query = query.Where(a => a.Edition == filter.Ausgabe);
        }
        else if (schuljahrEditions is { Count: > 0 })
        {
            query = query.Where(a => a.Edition != null && schuljahrEditions.Contains(a.Edition));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm;
            query = query.Where(a =>
                (a.SearchPatternIdu != null && a.SearchPatternIdu == term)
                || (a.SearchPatternTo != null && a.SearchPatternTo == term)
                || (a.PageTitle != null && EF.Functions.Like(a.PageTitle, $"%{term}%"))
                || (a.Url != null && EF.Functions.Like(a.Url, $"%{term}%")));
        }

        if (!string.IsNullOrWhiteSpace(filter.FileType))
        {
            query = query.Where(a => a.FileType == filter.FileType);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchPattern))
        {
            var pattern = filter.SearchPattern;
            query = query.Where(a =>
                a.SearchPatternIdu == pattern
                || a.SearchPatternTo == pattern);
        }

        return query;
    }
}
