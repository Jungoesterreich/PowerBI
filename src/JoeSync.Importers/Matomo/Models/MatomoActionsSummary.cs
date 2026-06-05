// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Text.Json.Serialization;

namespace JoeSync.Importers.Matomo.Models;

public sealed class MatomoActionsSummary
{
    [JsonPropertyName("nb_pageviews")]
    public int PageViews { get; set; }

    [JsonPropertyName("nb_uniq_pageviews")]
    public int UniquePageViews { get; set; }

    [JsonPropertyName("nb_downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("nb_uniq_downloads")]
    public int UniqueDownloads { get; set; }
}
