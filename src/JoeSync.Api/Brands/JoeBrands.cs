// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Brands;

public sealed record BrandDefinition(
    string Key,
    string Label,
    string UrlContains,
    string? UrlExcludes);

public static class JoeBrands
{
    public static readonly IReadOnlyList<BrandDefinition> All =
    [
        new("idu", "Ich-und-Du", "ich-und-du", null),
        new("jin", "Join-In", "join-in-", null),
        new("joe", "JÖ", "joe-", null),
        new("ms", "Mini-Spatzenpost", "mini-spatzenpost-", null),
        new("sp", "Spatzenpost", "spatzenpost-", "mini-spatzenpost"),
        new("lux", "Lux", "lux-", null),
        new("to", "Topic", "topic-", null),
    ];

    public static BrandDefinition? Find(string key) =>
        All.FirstOrDefault(b =>
            string.Equals(b.Key, key, StringComparison.OrdinalIgnoreCase));
}
