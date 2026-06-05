// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Text.Json.Serialization;

namespace JoeSync.Importers.Matomo.Models;

public sealed class MatomoActionDetail
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("pageTitle")]
    public string? PageTitle { get; set; }

    [JsonPropertyName("pageIdAction")]
    public int? PageIdAction { get; set; }

    [JsonPropertyName("idpageview")]
    public string IdPageview { get; set; } = "";

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }

    [JsonPropertyName("pageId")]
    public int? PageId { get; set; }

    [JsonPropertyName("timeSpent")]
    public int? TimeSpent { get; set; }

    [JsonPropertyName("pageLoadTimeMilliseconds")]
    public int? PageLoadTimeMilliseconds { get; set; }

    [JsonPropertyName("pageviewPosition")]
    public int? PageviewPosition { get; set; }

    [JsonPropertyName("eventCategory")]
    public string? EventCategory { get; set; }

    [JsonPropertyName("eventAction")]
    public string? EventAction { get; set; }

    [JsonPropertyName("eventName")]
    public string? EventName { get; set; }

    [JsonPropertyName("dimension2")]
    public string? Dimension2 { get; set; }

    [JsonPropertyName("dimension3")]
    public string? Dimension3 { get; set; }
}
