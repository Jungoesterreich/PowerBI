// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Core.Data.Entities;

public sealed class SyncLog
{
    public int Id { get; init; }

    public required string JobName { get; init; }

    public DateTime StartTime { get; init; }

    public DateTime? EndTime { get; set; }

    public required SyncLogStatus Status { get; set; }

    public int? RowsAffected { get; set; }

    public string? ErrorMessage { get; set; }
}

public enum SyncLogStatus
{
    Unknown = 0,
    Running = 1,
    Success = 21,
    Failed = 31,
}
