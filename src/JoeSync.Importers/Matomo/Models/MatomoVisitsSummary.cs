// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Text.Json.Serialization;

namespace JoeSync.Importers.Matomo.Models;

public sealed class MatomoVisitsSummary
{
    [JsonPropertyName("nb_visits")]
    public int Visits { get; set; }

    [JsonPropertyName("nb_uniq_visitors")]
    public int UniqueVisitors { get; set; }

    [JsonPropertyName("nb_actions")]
    public int Actions { get; set; }

    [JsonPropertyName("nb_pageviews")]
    public int PageViews { get; set; }

    [JsonPropertyName("nb_uniq_pageviews")]
    public int UniquePageViews { get; set; }

    [JsonPropertyName("nb_downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("nb_uniq_downloads")]
    public int UniqueDownloads { get; set; }

    [JsonPropertyName("bounce_count")]
    public int BounceCount { get; set; }

    [JsonPropertyName("sum_visit_length")]
    public int SumVisitLength { get; set; }

    [JsonPropertyName("nb_searches")]
    public int Searches { get; set; }

    [JsonPropertyName("nb_events")]
    public int Events { get; set; }
}
