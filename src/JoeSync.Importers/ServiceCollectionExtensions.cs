// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Contracts;
using JoeSync.Importers.Matomo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JoeSync.Importers;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJoeSyncImporters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["Matomo:BaseUrl"]
            ?? "https://jungoesterreich.matomo.cloud";

        services.AddHttpClient<MatomoApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddScoped<MatomoStagingWriter>();
        services.AddScoped<ISyncJob, MatomoImporter>();

        services.AddScoped<MatomoVisitsSummaryStagingWriter>();
        services.AddScoped<ISyncJob, MatomoVisitsSummaryImporter>();

        services.AddScoped<MatomoMediaAudioStagingWriter>();
        services.AddScoped<ISyncJob, MatomoMediaAudioImporter>();

        return services;
    }
}
