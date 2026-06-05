// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Text.Json.Serialization;

namespace JoeSync.Importers.Matomo.Models;

public sealed class MatomoVisit
{
    [JsonPropertyName("idSite")]
    public int IdSite { get; set; }

    [JsonPropertyName("idVisit")]
    public long IdVisit { get; set; }

    [JsonPropertyName("visitorId")]
    public string? VisitorId { get; set; }

    [JsonPropertyName("visitIp")]
    public string? VisitIp { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("serverDate")]
    public string ServerDate { get; set; } = "";

    [JsonPropertyName("serverTimestamp")]
    public long? ServerTimestamp { get; set; }

    [JsonPropertyName("firstActionTimestamp")]
    public long? FirstActionTimestamp { get; set; }

    [JsonPropertyName("lastActionTimestamp")]
    public long? LastActionTimestamp { get; set; }

    [JsonPropertyName("visitDuration")]
    public int? VisitDuration { get; set; }

    [JsonPropertyName("actions")]
    public int? Actions { get; set; }

    [JsonPropertyName("searches")]
    public int? Searches { get; set; }

    [JsonPropertyName("events")]
    public int? Events { get; set; }

    [JsonPropertyName("interactions")]
    public int? Interactions { get; set; }

    [JsonPropertyName("visitorType")]
    public string? VisitorType { get; set; }

    [JsonPropertyName("visitCount")]
    public int? VisitCount { get; set; }

    [JsonPropertyName("daysSinceFirstVisit")]
    public int? DaysSinceFirstVisit { get; set; }

    [JsonPropertyName("daysSinceLastVisit")]
    public int? DaysSinceLastVisit { get; set; }

    [JsonPropertyName("referrerType")]
    public string? ReferrerType { get; set; }

    [JsonPropertyName("referrerName")]
    public string? ReferrerName { get; set; }

    [JsonPropertyName("referrerUrl")]
    public string? ReferrerUrl { get; set; }

    [JsonPropertyName("referrerKeyword")]
    public string? ReferrerKeyword { get; set; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; set; }

    [JsonPropertyName("deviceBrand")]
    public string? DeviceBrand { get; set; }

    [JsonPropertyName("deviceModel")]
    public string? DeviceModel { get; set; }

    [JsonPropertyName("operatingSystem")]
    public string? OperatingSystem { get; set; }

    [JsonPropertyName("operatingSystemName")]
    public string? OperatingSystemName { get; set; }

    [JsonPropertyName("operatingSystemCode")]
    public string? OperatingSystemCode { get; set; }

    [JsonPropertyName("operatingSystemVersion")]
    public string? OperatingSystemVersion { get; set; }

    [JsonPropertyName("browser")]
    public string? Browser { get; set; }

    [JsonPropertyName("browserName")]
    public string? BrowserName { get; set; }

    [JsonPropertyName("browserCode")]
    public string? BrowserCode { get; set; }

    [JsonPropertyName("browserVersion")]
    public string? BrowserVersion { get; set; }

    [JsonPropertyName("browserFamily")]
    public string? BrowserFamily { get; set; }

    [JsonPropertyName("browserFamilyDescription")]
    public string? BrowserFamilyDescription { get; set; }

    [JsonPropertyName("continent")]
    public string? Continent { get; set; }

    [JsonPropertyName("continentCode")]
    public string? ContinentCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("regionCode")]
    public string? RegionCode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("latitude")]
    public string? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public string? Longitude { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("languageCode")]
    public string? LanguageCode { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("campaignId")]
    public string? CampaignId { get; set; }

    [JsonPropertyName("campaignName")]
    public string? CampaignName { get; set; }

    [JsonPropertyName("campaignKeyword")]
    public string? CampaignKeyword { get; set; }

    [JsonPropertyName("campaignContent")]
    public string? CampaignContent { get; set; }

    [JsonPropertyName("campaignSource")]
    public string? CampaignSource { get; set; }

    [JsonPropertyName("campaignMedium")]
    public string? CampaignMedium { get; set; }

    [JsonPropertyName("visitConverted")]
    public int? VisitConverted { get; set; }

    [JsonPropertyName("goalConversions")]
    public int? GoalConversions { get; set; }

    [JsonPropertyName("formConversions")]
    public int? FormConversions { get; set; }

    [JsonPropertyName("actionDetails")]
    public List<MatomoActionDetail> ActionDetails { get; set; } = [];
}
