// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using JoeSync.Importers.Matomo.Models;
using Microsoft.Extensions.Logging;

namespace JoeSync.Importers.Matomo;

public sealed class MatomoApiClient
{
    private const int BatchSize = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<MatomoApiClient> _logger;

    public MatomoApiClient(
        HttpClient httpClient,
        ILogger<MatomoApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<MatomoVisit>> FetchVisitsForDateAsync(
        int idSite,
        DateOnly date,
        string token,
        CancellationToken cancellationToken = default)
    {
        var allVisits = new List<MatomoVisit>();
        var offset = 0;
        var dateStr = date.ToString("yyyy-MM-dd");

        while (true)
        {
            var url = $"index.php?module=API"
                + $"&method=Live.getLastVisitsDetails"
                + $"&idSite={idSite}"
                + $"&period=day"
                + $"&date={dateStr}"
                + $"&format=json"
                + $"&token_auth={token}"
                + $"&filter_limit={BatchSize}"
                + $"&filter_offset={offset}";

            _logger.LogDebug(
                "Fetching site {IdSite} date {Date} offset {Offset}",
                idSite, dateStr, offset);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var batch = JsonSerializer.Deserialize<List<MatomoVisit>>(
                json, JsonOptions) ?? [];

            if (batch.Count == 0)
            {
                break;
            }

            allVisits.AddRange(batch);

            _logger.LogDebug(
                "Got {BatchCount} visits (total: {TotalCount})",
                batch.Count, allVisits.Count);

            if (batch.Count < BatchSize)
            {
                break;
            }

            offset += BatchSize;
        }

        _logger.LogInformation(
            "Fetched {Count} visits for site {IdSite} on {Date}",
            allVisits.Count, idSite, dateStr);

        return allVisits;
    }

    public async Task<MatomoVisitsSummary?> GetVisitsSummaryAsync(
        int idSite,
        DateOnly date,
        string token,
        CancellationToken cancellationToken = default)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var url = $"index.php?module=API"
            + $"&method=VisitsSummary.get"
            + $"&idSite={idSite}"
            + $"&period=day"
            + $"&date={dateStr}"
            + $"&format=json"
            + $"&token_auth={token}";

        _logger.LogDebug(
            "Fetching visits summary for site {IdSite} on {Date}",
            idSite, dateStr);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        // Matomo returns [] (array) instead of {} (object) when no data
        if (string.IsNullOrWhiteSpace(json)
            || json.TrimStart().StartsWith('['))
        {
            _logger.LogDebug(
                "No visits summary data for site {IdSite} on {Date}",
                idSite, dateStr);
            return null;
        }

        var summary = JsonSerializer.Deserialize<MatomoVisitsSummary>(
            json, JsonOptions);

        if (summary is not null)
        {
            // VisitsSummary.get does not return nb_uniq_pageviews /
            // nb_uniq_downloads. Those come from Actions.get — merge them
            // into the same DTO so the aggregation SP can use them.
            var actions = await GetActionsAsync(
                idSite, date, token, cancellationToken);

            if (actions is not null)
            {
                summary.PageViews = actions.PageViews;
                summary.UniquePageViews = actions.UniquePageViews;
                summary.Downloads = actions.Downloads;
                summary.UniqueDownloads = actions.UniqueDownloads;
            }
        }

        _logger.LogInformation(
            "Fetched visits summary for site {IdSite} on {Date}: {Visits} visits",
            idSite, dateStr, summary?.Visits ?? 0);

        return summary;
    }

    private async Task<MatomoActionsSummary?> GetActionsAsync(
        int idSite,
        DateOnly date,
        string token,
        CancellationToken cancellationToken)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var url = $"index.php?module=API"
            + $"&method=Actions.get"
            + $"&idSite={idSite}"
            + $"&period=day"
            + $"&date={dateStr}"
            + $"&format=json"
            + $"&token_auth={token}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(json)
            || json.TrimStart().StartsWith('['))
        {
            return null;
        }

        return JsonSerializer.Deserialize<MatomoActionsSummary>(
            json, JsonOptions);
    }

    public async Task<List<MatomoMediaAudio>> GetMediaAudioAsync(
        int idSite,
        DateOnly date,
        string token,
        CancellationToken cancellationToken = default)
    {
        var audio = await FetchMediaResourcesAsync(
            "MediaAnalytics.getAudioResources", idSite, date, token, cancellationToken);

        var video = await FetchMediaResourcesAsync(
            "MediaAnalytics.getVideoResources", idSite, date, token, cancellationToken);

        var all = new List<MatomoMediaAudio>(audio.Count + video.Count);
        all.AddRange(audio);
        all.AddRange(video);

        _logger.LogInformation(
            "Fetched {AudioCount} audio + {VideoCount} video resources for site {IdSite} on {Date}",
            audio.Count, video.Count, idSite, date.ToString("yyyy-MM-dd"));

        return all;
    }

    private async Task<List<MatomoMediaAudio>> FetchMediaResourcesAsync(
        string method,
        int idSite,
        DateOnly date,
        string token,
        CancellationToken cancellationToken = default)
    {
        var allMedia = new List<MatomoMediaAudio>();
        var offset = 0;
        var dateStr = date.ToString("yyyy-MM-dd");

        while (true)
        {
            var url = $"index.php?module=API"
                + $"&method={method}"
                + $"&idSite={idSite}"
                + $"&period=day"
                + $"&date={dateStr}"
                + $"&format=json"
                + $"&flat=1"
                + $"&token_auth={token}"
                + $"&filter_limit={BatchSize}"
                + $"&filter_offset={offset}";

            _logger.LogDebug(
                "Fetching {Method} for site {IdSite} date {Date} offset {Offset}",
                method, idSite, dateStr, offset);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Raw response for {Method} site {IdSite}: {Json}",
                method, idSite, json.Length > 500 ? json[..500] : json);

            // Matomo returns {} (object) instead of [] (array) when no data
            if (string.IsNullOrWhiteSpace(json)
                || json.TrimStart().StartsWith('{'))
            {
                break;
            }

            var batch = JsonSerializer.Deserialize<List<MatomoMediaAudio>>(
                json, JsonOptions) ?? [];

            if (batch.Count == 0)
            {
                break;
            }

            allMedia.AddRange(batch);

            if (batch.Count < BatchSize)
            {
                break;
            }

            offset += BatchSize;
        }

        return allMedia;
    }
}
