// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Dtos;

public sealed record SyncJobDto(string JobName);

public sealed record SyncStatusDto(
    bool IsRunning,
    string? JobName,
    long? SyncLogId,
    DateTime? StartedAt,
    string? OverrideFrom,
    string? OverrideTo);

public sealed record SyncLogDto(
    int Id,
    string JobName,
    DateTime StartTime,
    DateTime? EndTime,
    string Status,
    int? RowsAffected,
    string? ErrorMessage,
    int? DurationSeconds);

public sealed record RunResponseDto(
    IReadOnlyList<RunResponseItem> Items);

public sealed record RunResponseItem(string JobName, int SyncLogId);

public sealed record RunRequestDto(
    string? From,
    string? To);
