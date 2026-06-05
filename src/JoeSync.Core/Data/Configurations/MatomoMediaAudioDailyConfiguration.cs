// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoeSync.Core.Data.Configurations;

public sealed class MatomoMediaAudioDailyConfiguration
    : IEntityTypeConfiguration<MatomoMediaAudioDaily>
{
    public void Configure(EntityTypeBuilder<MatomoMediaAudioDaily> builder)
    {
        builder.ToTable("matomo_media_audio_daily", "curated");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();

        builder.Property(e => e.SiteName).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(2000);
        builder.Property(e => e.Url).HasMaxLength(2000);
        builder.Property(e => e.UrlLastPathElement).HasMaxLength(500);
        builder.Property(e => e.Edition).HasMaxLength(50);

        builder.HasIndex(e => new { e.IdSite, e.Date, e.Label });
    }
}
