// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoeSync.Core.Data.Configurations;

public sealed class MatomoDailySummaryConfiguration
    : IEntityTypeConfiguration<MatomoDailySummary>
{
    public void Configure(EntityTypeBuilder<MatomoDailySummary> builder)
    {
        builder.ToTable("matomo_daily_summary", "curated");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();

        builder.Property(e => e.SiteName).HasMaxLength(50).IsRequired();

        builder.HasIndex(e => new { e.IdSite, e.Date }).IsUnique();
    }
}
