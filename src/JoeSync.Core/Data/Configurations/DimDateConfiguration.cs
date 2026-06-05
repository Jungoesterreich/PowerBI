// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JoeSync.Core.Data.Configurations;

public sealed class DimDateConfiguration
    : IEntityTypeConfiguration<DimDate>
{
    public void Configure(EntityTypeBuilder<DimDate> builder)
    {
        builder.ToTable("dim_date", "curated");

        builder.HasKey(e => e.Date);
        builder.Property(e => e.Date).HasColumnType("date");

        builder.Property(e => e.MonthName).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Month).HasMaxLength(20).IsRequired();
    }
}
