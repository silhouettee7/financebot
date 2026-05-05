using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.BackgroundServices;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Impl.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafka(
        this IServiceCollection serviceCollection,
        Action<KafkaGlobalSettings> configure)
    {
        var settings = new KafkaGlobalSettings();
        configure(settings);
        serviceCollection.AddSingleton(settings);
        
        return serviceCollection;
    }

    public static IServiceCollection AddProducerGeneral(
        this IServiceCollection serviceCollection,
        Action<ProducerSettingsGeneral> configure)
    {
        var settings = new ProducerSettingsGeneral();
        configure(settings);
        serviceCollection.AddSingleton(settings);
        var config = new ProducerConfig();
        var producer = new ProducerBuilder<byte[]?, byte[]>(
                config.FromProducerSettingsGeneral(settings))
            .Build();
        serviceCollection.AddSingleton(producer);
        serviceCollection.AddSingleton<IAsyncProducerProvider,DefaultProducerProvider>();
        serviceCollection.AddSingleton<IProducerContext, ProducerContext>();
        
        return serviceCollection;
    }
    
    public static IServiceCollection AddProducer<TKey, TValue, TTopic>(
        this IServiceCollection serviceCollection,
        Action<ProducerSettings<TKey, TValue, TTopic>> configure,
        Action<ProducerSettingsGeneral>? overrideConfigure = null) where TTopic : ITopic
    {
        var producerSettings = new ProducerSettings<TKey, TValue, TTopic>();
        configure(producerSettings);
        serviceCollection.AddSingleton<RegistrationProducer<TKey, TValue, TTopic>>(sp =>
            {
                var producer = sp.GetService<IProducer<byte[]?, byte[]>>();
                var generalSettings = sp.GetService<ProducerSettingsGeneral>();
                if (producer is null || generalSettings is null)
                { 
                    throw new InvalidOperationException($"Нет настройки продюсеров методом {nameof(AddProducerGeneral)}");
                }
                
                if (overrideConfigure is not null)
                {
                    var newGeneralSettings = new ProducerSettingsGeneral();
                    overrideConfigure(newGeneralSettings);
                    generalSettings = newGeneralSettings;
                    var config = new ProducerConfig();
                    producer = new ProducerBuilder<byte[]?, byte[]>(
                            config.FromProducerSettingsGeneral(generalSettings))
                        .Build();
                }

                producerSettings.GeneralSettings = generalSettings;
                
                return new RegistrationProducer<TKey, TValue, TTopic>
                {
                    Settings = producerSettings,
                    Producer = producer
                };
            });
        
        return serviceCollection;
    }

    public static IServiceCollection AddProducer<TValue, TTopic>(
        this IServiceCollection serviceCollection,
        Action<ProducerSettings<Null, TValue, TTopic>> configure,
        Action<ProducerSettingsGeneral>? overrideConfigure = null) where TTopic : ITopic
    {
        serviceCollection.AddProducer<Null, TValue, TTopic>(configure, overrideConfigure);
        
        return serviceCollection;
    }

    public static IServiceCollection AddConsumer<TKey, TValue, THandler>(
        this IServiceCollection serviceCollection,
        Action<ConsumerSettings> configure) where THandler: class, IMessageHandler<TKey, TValue>
    {
        var config = new ConsumerSettings();
        configure(config);
        var consumer = new ConsumerBuilder<TKey, TValue>(
                new ConsumerConfig().FromConsumerSettings(config))
            .Build();
        var registrationConsumer = new RegistrationConsumer<TKey, TValue>
        {
            Settings = config,
            Consumer = consumer
        };
        serviceCollection.AddSingleton<RegistrationConsumer<TKey, TValue>>(_ => registrationConsumer);
        serviceCollection.AddSingleton<THandler>();
        serviceCollection.AddHostedService<ConsumerService<TKey, TValue, THandler>>(sp =>
        {
            var handler = sp.GetService<THandler>()!;
            var logger = sp.GetService<ILogger<ConsumerService<TKey, TValue, THandler>>>()!;
            
            return new ConsumerService<TKey, TValue, THandler>(
                registrationConsumer, handler, logger);
        });
        
        return serviceCollection;
    }
    
    public static IServiceCollection AddTransactionConsumer<TKey, TValue, THandler>(
        this IServiceCollection serviceCollection,
        Action<ConsumerSettings> configure) 
        where THandler: class, ITransactionMessageHandler<TKey, TValue>
    {
        var config = new ConsumerSettings();
        configure(config);
        var consumer = new ConsumerBuilder<TKey, TValue>(
            new ConsumerConfig().FromConsumerSettings(config))
            .Build();
        var registrationConsumer = new RegistrationConsumer<TKey, TValue>
        {
            Settings = config,
            Consumer = consumer
        };
        serviceCollection.AddSingleton<RegistrationConsumer<TKey, TValue>>(_ => registrationConsumer);
        serviceCollection.AddSingleton<THandler>();
        serviceCollection.AddHostedService<TransactionalConsumerService<TKey, TValue, THandler>>(sp =>
        {
            var logger = sp.GetService<ILogger<TransactionalConsumerService<TKey, TValue, THandler>>>()!;
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            var handler = sp.GetService<THandler>()!;
            var producer = sp.GetService<IProducer<byte[]?, byte[]>>();

            if (producer is null || globalSettings is null)
            {
                throw new InvalidOperationException($"Не добавлена конфигурация продюсера {nameof(AddProducerGeneral)}" +
                                                    $" или глобальная {nameof(AddKafka)}");
            }

            return new TransactionalConsumerService<TKey, TValue, THandler>(
                handler, globalSettings, registrationConsumer, sp, producer, logger);
        });

        return serviceCollection;
    }
    
    public static IServiceCollection AddConsumer<TValue, THandler>(
        this IServiceCollection serviceCollection,
        Action<ConsumerSettings> configure) where THandler: class, IMessageHandler<Null, TValue>
    {
        serviceCollection.AddConsumer<Null,TValue,THandler>(configure);
        
        return serviceCollection;
    }
    
    public static IServiceCollection AddTransactionConsumer<TValue, THandler>(
        this IServiceCollection serviceCollection,
        Action<ConsumerSettings> configure) 
        where THandler: class, ITransactionMessageHandler<Null, TValue>
    {
        serviceCollection.AddTransactionConsumer<Null,TValue,THandler>(configure);
        
        return serviceCollection;
    }
}