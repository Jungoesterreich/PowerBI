// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Text.Json.Serialization;

namespace JoeSync.Importers.Matomo.Models;

public sealed class MatomoMediaAudio
{
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("nb_plays")]
    public int Plays { get; set; }

    [JsonPropertyName("nb_unique_visitors_plays")]
    public int UniqueVisitorsPlays { get; set; }

    [JsonPropertyName("nb_impressions")]
    public int Impressions { get; set; }

    [JsonPropertyName("nb_unique_visitors_impressions")]
    public int UniqueVisitorsImpressions { get; set; }

    [JsonPropertyName("nb_finishes")]
    public int Finishes { get; set; }

    [JsonPropertyName("play_rate")]
    public double PlayRate { get; set; }

    [JsonPropertyName("finish_rate")]
    public double FinishRate { get; set; }

    [JsonPropertyName("avg_time_watched")]
    public int AvgTimeWatched { get; set; }

    [JsonPropertyName("avg_completion")]
    public double AvgCompletion { get; set; }

    [JsonPropertyName("avg_time_to_play")]
    public int AvgTimeToPlay { get; set; }

    [JsonPropertyName("avg_media_length")]
    public int AvgMediaLength { get; set; }
}
