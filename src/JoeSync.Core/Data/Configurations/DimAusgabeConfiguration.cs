// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoeSync.Core.Data.Configurations;

public sealed class DimAusgabeConfiguration
    : IEntityTypeConfiguration<DimAusgabe>
{
    public void Configure(EntityTypeBuilder<DimAusgabe> builder)
    {
        builder.ToTable("dim_ausgaben", "curated");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();

        builder.Property(e => e.Ausgabenkuerzel).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Schuljahr).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => e.Ausgabenkuerzel).IsUnique();
    }
}
