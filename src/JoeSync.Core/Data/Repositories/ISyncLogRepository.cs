// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;

namespace JoeSync.Core.Data.Repositories;

public interface ISyncLogRepository
{
    Task<SyncLog> LogStartAsync(
        string jobName,
        CancellationToken cancellationToken = default);

    Task LogSuccessAsync(
        SyncLog log,
        int rowsAffected,
        CancellationToken cancellationToken = default);

    Task LogFailureAsync(
        SyncLog log,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetLastSuccessfulRunAsync(
        string jobName,
        CancellationToken cancellationToken = default);
}
