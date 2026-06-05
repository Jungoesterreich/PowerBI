// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Importers.Matomo;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoeSync.Importers.Tests;

public sealed class MatomoApiClientTests
{
    private static readonly DateOnly TestDate = new(2026, 1, 1);

    private static MatomoApiClient CreateClient(StubHttpMessageHandler handler)
    {
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://matomo.test/"),
        };

        return new MatomoApiClient(http, NullLogger<MatomoApiClient>.Instance);
    }

    private static string VisitsArray(IEnumerable<long> ids)
    {
        var items = ids.Select(id =>
            $"{{\"idVisit\":{id},\"idSite\":1,\"serverDate\":\"2026-01-01\"}}");
        return "[" + string.Join(",", items) + "]";
    }

    [Fact]
    public async Task FetchVisits_SinglePageBelowBatchSize_StopsAfterOneRequest()
    {
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.Json(VisitsArray([1, 2])));
        var client = CreateClient(handler);

        var visits = await client.FetchVisitsForDateAsync(1, TestDate, "token");

        Assert.Equal(2, visits.Count);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task FetchVisits_EmptyArray_ReturnsEmptyAndStops()
    {
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.Json("[]"));
        var client = CreateClient(handler);

        var visits = await client.FetchVisitsForDateAsync(1, TestDate, "token");

        Assert.Empty(visits);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task FetchVisits_FullFirstPage_PagesUntilPartialPage()
    {
        // First page is exactly BatchSize (1000) → client must request the next
        // offset; second page is partial (< 1000) → loop terminates.
        var fullPage = VisitsArray(Enumerable.Range(0, 1000).Select(i => (long)i));
        var partialPage = VisitsArray([9001, 9002, 9003]);

        var handler = new StubHttpMessageHandler(req =>
            req.RequestUri!.ToString().EndsWith("filter_offset=0", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json(fullPage)
                : StubHttpMessageHandler.Json(partialPage));
        var client = CreateClient(handler);

        var visits = await client.FetchVisitsForDateAsync(1, TestDate, "token");

        Assert.Equal(1003, visits.Count);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task GetVisitsSummary_EmptyArrayResponse_ReturnsNullWithoutActionsCall()
    {
        // Matomo emits [] instead of {} when there is no data for the day.
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.Json("[]"));
        var client = CreateClient(handler);

        var summary = await client.GetVisitsSummaryAsync(1, TestDate, "token");

        Assert.Null(summary);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetVisitsSummary_MergesActionsIntoSummary()
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri.Contains("method=VisitsSummary.get", StringComparison.Ordinal))
            {
                return StubHttpMessageHandler.Json(
                    "{\"nb_visits\":42,\"nb_uniq_visitors\":30}");
            }

            return StubHttpMessageHandler.Json(
                "{\"nb_pageviews\":100,\"nb_uniq_pageviews\":80,"
                + "\"nb_downloads\":5,\"nb_uniq_downloads\":4}");
        });
        var client = CreateClient(handler);

        var summary = await client.GetVisitsSummaryAsync(1, TestDate, "token");

        Assert.NotNull(summary);
        Assert.Equal(42, summary!.Visits);
        Assert.Equal(30, summary.UniqueVisitors);
        // The four action metrics are not part of VisitsSummary.get and must be
        // merged from Actions.get.
        Assert.Equal(100, summary.PageViews);
        Assert.Equal(80, summary.UniquePageViews);
        Assert.Equal(5, summary.Downloads);
        Assert.Equal(4, summary.UniqueDownloads);
    }

    [Fact]
    public async Task GetVisitsSummary_ReadsNumbersFromJsonStrings()
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            return uri.Contains("method=VisitsSummary.get", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json("{\"nb_visits\":\"42\"}")
                : StubHttpMessageHandler.Json("[]");
        });
        var client = CreateClient(handler);

        var summary = await client.GetVisitsSummaryAsync(1, TestDate, "token");

        Assert.NotNull(summary);
        Assert.Equal(42, summary!.Visits);
    }

    [Fact]
    public async Task GetMediaAudio_CombinesAudioAndVideoResources()
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            if (uri.Contains("getAudioResources", StringComparison.Ordinal))
            {
                return StubHttpMessageHandler.Json(
                    "[{\"label\":\"a1\",\"nb_plays\":5},"
                    + "{\"label\":\"a2\",\"nb_plays\":3}]");
            }

            return StubHttpMessageHandler.Json(
                "[{\"label\":\"v1\",\"nb_plays\":9}]");
        });
        var client = CreateClient(handler);

        var media = await client.GetMediaAudioAsync(1, TestDate, "token");

        Assert.Equal(3, media.Count);
    }

    [Fact]
    public async Task GetMediaAudio_EmptyObjectResponse_ReturnsEmpty()
    {
        // For media endpoints Matomo emits {} (object) instead of [] when empty.
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.Json("{}"));
        var client = CreateClient(handler);

        var media = await client.GetMediaAudioAsync(1, TestDate, "token");

        Assert.Empty(media);
    }
}
