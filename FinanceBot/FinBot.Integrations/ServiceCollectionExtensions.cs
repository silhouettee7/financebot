using FinBot.Bll.Interfaces.Integration;
using FinBot.Integrations.Cache;
using FinBot.Integrations.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Integrations;

public static class ServiceCollectionExtensions
{
    public static void AddKafkaIntegration(this IServiceCollection services)
    {
        services.AddSingleton<IReportProducer, KafkaProducer>();
    }

    public static IServiceCollection AddRedisCacheIntegration(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var connectionString = configuration["App:Redis:RedisCacheConnection"];
        var prefix = configuration["App:Redis:RedisCachePrefix"];

        serviceCollection.AddStackExchangeRedisCache(option =>
        {
            option.Configuration = connectionString;
            option.InstanceName = prefix;
        });

        serviceCollection.AddSingleton<ICacheStorage, CacheStorage>();

        return serviceCollection;
    }
}