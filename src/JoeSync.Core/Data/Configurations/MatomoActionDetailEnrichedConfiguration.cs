// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoeSync.Core.Data.Configurations;

public sealed class MatomoActionDetailEnrichedConfiguration
    : IEntityTypeConfiguration<MatomoActionDetailEnriched>
{
    public void Configure(EntityTypeBuilder<MatomoActionDetailEnriched> builder)
    {
        builder.ToTable("matomo_action_details_enriched", "curated");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();

        builder.Property(e => e.SiteName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.Url).HasColumnType("nvarchar(max)");
        builder.Property(e => e.PageTitle).HasMaxLength(1000);
        builder.Property(e => e.UrlLastPathElement).HasColumnType("nvarchar(max)");
        builder.Property(e => e.UrlParameter).HasColumnType("nvarchar(max)");
        builder.Property(e => e.UrlClean).HasColumnType("nvarchar(max)");
        builder.Property(e => e.UrlPathFull).HasColumnType("nvarchar(max)");
        builder.Property(e => e.UrlPathElementOne).HasColumnType("nvarchar(max)");
        builder.Property(e => e.UrlPathElementTwo).HasColumnType("nvarchar(max)");
        builder.Property(e => e.FileType).HasMaxLength(20);
        builder.Property(e => e.Edition).HasMaxLength(50);
        builder.Property(e => e.SearchPatternIdu).HasMaxLength(100);
        builder.Property(e => e.SearchPatternTo).HasMaxLength(100);
        builder.Property(e => e.Seitenart).HasMaxLength(100);
        builder.Property(e => e.SubRubrik).HasMaxLength(200);
        builder.Property(e => e.ServerDate).HasColumnType("date");
        builder.Property(e => e.VisitorId).HasMaxLength(50);
        builder.Property(e => e.DeviceType).HasMaxLength(50);
        builder.Property(e => e.Browser).HasMaxLength(100);
        builder.Property(e => e.Country).HasMaxLength(100);
        builder.Property(e => e.City).HasMaxLength(100);

        builder.HasIndex(e => new { e.IdSite, e.IdVisit });
        builder.HasIndex(e => new { e.IdSite, e.Timestamp });
    }
}
