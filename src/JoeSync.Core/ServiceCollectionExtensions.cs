// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data;
using JoeSync.Core.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JoeSync.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJoeSyncCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("JoeDB");

        services.AddDbContext<JoeSyncDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ISyncLogRepository, SyncLogRepository>();

        return services;
    }
}
