// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Core.Data.Entities;

public sealed class DimAusgabe
{
    public int Id { get; init; }

    public required string Ausgabenkuerzel { get; init; }

    public required string Schuljahr { get; init; }

    public int? Jahr { get; set; }

    public int? Ausgabe { get; set; }

    public int? AusgabeIDU { get; set; }
}
