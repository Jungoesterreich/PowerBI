// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Core.Contracts;

public interface ISyncJob
{
    string JobName { get; }

    Task<SyncResult> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed record SyncResult
{
    public SyncResult(bool Success,
        int RowsAffected,
        string? ErrorMessage = null)
    {
        this.Success = Success;
        this.RowsAffected = RowsAffected;
        this.ErrorMessage = ErrorMessage;
    }

    public static SyncResult Ok(int rowsAffected) => new(true, rowsAffected);

    public static SyncResult Fail(string errorMessage) => new(false, 0, errorMessage);
    public bool Success { get; init; }
    public int RowsAffected { get; init; }
    public string? ErrorMessage { get; init; }

    public void Deconstruct(
        out bool Success,
        out int RowsAffected,
        out string? ErrorMessage)
    {
        Success = this.Success;
        RowsAffected = this.RowsAffected;
        ErrorMessage = this.ErrorMessage;
    }
}
