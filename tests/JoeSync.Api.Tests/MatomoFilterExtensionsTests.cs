// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Filters;
using JoeSync.Core.Data.Entities;

namespace JoeSync.Api.Tests;

/// <summary>
/// Exercises the provider-agnostic filter predicates of
/// <see cref="MatomoFilterExtensions.ApplyCommonFilters"/> via LINQ-to-Objects.
/// The free-text <c>SearchTerm</c> branch relies on <c>EF.Functions.Like</c> and
/// is therefore only translatable by an EF provider, so it is excluded here.
/// </summary>
public sealed class MatomoFilterExtensionsTests
{
    private static MatomoActionDetailEnriched Row(
        int idSite = 1,
        string? edition = null,
        string? fileType = null,
        DateTime? serverDate = null,
        string? searchPatternIdu = null,
        string? searchPatternTo = null)
    {
        return new MatomoActionDetailEnriched
        {
            IdSite = idSite,
            SiteName = "site",
            Type = "action",
            Edition = edition,
            FileType = fileType,
            ServerDate = serverDate,
            SearchPatternIdu = searchPatternIdu,
            SearchPatternTo = searchPatternTo,
        };
    }

    private static IQueryable<MatomoActionDetailEnriched> Sample()
    {
        return new List<MatomoActionDetailEnriched>
        {
            Row(idSite: 1, edition: "A", fileType: "pdf", serverDate: new DateTime(2026, 1, 10), searchPatternIdu: "idu1"),
            Row(idSite: 2, edition: "B", fileType: "mp3", serverDate: new DateTime(2026, 1, 20), searchPatternTo: "to2"),
            Row(idSite: 1, edition: "C", fileType: "pdf", serverDate: new DateTime(2026, 2, 1)),
        }.AsQueryable();
    }

    private static MatomoQueryFilter Filter(
        int? siteId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? schuljahr = null,
        string? ausgabe = null,
        string? fileType = null,
        string? searchPattern = null)
    {
        return new MatomoQueryFilter(
            siteId, from, to, schuljahr, ausgabe, SearchTerm: null, fileType, searchPattern);
    }

    [Fact]
    public void NoFilters_ReturnsAllRows()
    {
        var result = Sample().ApplyCommonFilters(Filter(), null).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void SiteId_FiltersBySite()
    {
        var result = Sample().ApplyCommonFilters(Filter(siteId: 1), null).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(1, r.IdSite));
    }

    [Fact]
    public void DateRange_IsInclusiveOnBothEnds()
    {
        var result = Sample()
            .ApplyCommonFilters(
                Filter(from: new DateTime(2026, 1, 10), to: new DateTime(2026, 1, 20)),
                null)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.InRange(
            r.ServerDate!.Value, new DateTime(2026, 1, 10), new DateTime(2026, 1, 20)));
    }

    [Fact]
    public void Ausgabe_FiltersByExactEdition()
    {
        var result = Sample().ApplyCommonFilters(Filter(ausgabe: "A"), null).ToList();

        Assert.Single(result);
        Assert.Equal("A", result[0].Edition);
    }

    [Fact]
    public void SchuljahrEditions_FilterWhenAusgabeNotGiven()
    {
        var result = Sample()
            .ApplyCommonFilters(Filter(), new[] { "A", "B" })
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, r => r.Edition == "C");
    }

    [Fact]
    public void Ausgabe_TakesPrecedenceOverSchuljahrEditions()
    {
        var result = Sample()
            .ApplyCommonFilters(Filter(ausgabe: "A"), new[] { "B", "C" })
            .ToList();

        Assert.Single(result);
        Assert.Equal("A", result[0].Edition);
    }

    [Fact]
    public void FileType_FiltersByExactType()
    {
        var result = Sample().ApplyCommonFilters(Filter(fileType: "pdf"), null).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("pdf", r.FileType));
    }

    [Fact]
    public void SearchPattern_MatchesEitherIduOrTo()
    {
        var iduResult = Sample().ApplyCommonFilters(Filter(searchPattern: "idu1"), null).ToList();
        var toResult = Sample().ApplyCommonFilters(Filter(searchPattern: "to2"), null).ToList();

        Assert.Single(iduResult);
        Assert.Equal("A", iduResult[0].Edition);
        Assert.Single(toResult);
        Assert.Equal("B", toResult[0].Edition);
    }
}
