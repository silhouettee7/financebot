using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.BackgroundServices;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Impl.Providers;
using FinBot.Kafka.Utils;
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
        Action<ProducerSettingsGeneral>? configure = null)
    {
        var settings = new ProducerSettingsGeneral();
        if (configure is not null)
        {
            configure(settings);
        }
        serviceCollection.AddSingleton(settings);
        var config = new ProducerConfig();
        serviceCollection.AddSingleton(sp =>
        {
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            if (globalSettings is null)
            {
                throw new InvalidOperationException("Глобальная конфигурация не добавлена");
            }
            return new ProducerBuilder<byte[]?, byte[]>(
                    config.FromProducerSettingsGeneral(settings, globalSettings))
                .Build();
        });
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
                var globalSettings = sp.GetService<KafkaGlobalSettings>();
                
                if (globalSettings is null)
                {
                    throw new InvalidOperationException("Глобальная конфигурация не добавлена");
                }
                
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
                            config.FromProducerSettingsGeneral(generalSettings, globalSettings))
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
        Action<ConsumerSettings<THandler>> configure) where THandler: class, IMessageHandler<TKey, TValue>
    {
        var config = new ConsumerSettings<THandler>();
        configure(config);
        serviceCollection.AddSingleton<THandler>();
        
        serviceCollection.AddSingleton<RegistrationConsumer<TKey, TValue, THandler>>(sp =>
        {
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            var handler = sp.GetService<THandler>()!;
            if (globalSettings is null)
            {
                throw new InvalidOperationException("Глобальная конфигурация не добавлена");
            }
            config.Handler = handler;
            var consumer = new ConsumerBuilder<TKey, TValue>(
                    new ConsumerConfig().FromConsumerSettings(config, globalSettings))
                .SetKeyDeserializer(new JsonDeserializer<TKey>())
                .SetValueDeserializer(new JsonDeserializer<TValue>())
                .Build();
            
            return new RegistrationConsumer<TKey, TValue, THandler>
            {
                Settings = config,
                Consumer = consumer
            };
        });
        serviceCollection.AddHostedService<ConsumerService<TKey, TValue, THandler>>(sp =>
        {
            var logger = sp.GetService<ILogger<ConsumerService<TKey, TValue, THandler>>>()!;
            var registrationConsumer = sp.GetService<RegistrationConsumer<TKey, TValue, THandler>>()!;
            return new ConsumerService<TKey, TValue, THandler>(
                registrationConsumer, logger);
        });
        
        return serviceCollection;
    }
    
    public static IServiceCollection AddTransactionConsumer<TKey, TValue, THandler>(
        this IServiceCollection serviceCollection,
        Action<ConsumerSettings<THandler>> configure) 
        where THandler: class, ITransactionMessageHandler<TKey, TValue>
    {
        var config = new ConsumerSettings<THandler>();
        configure(config);
        serviceCollection.AddSingleton<THandler>();
        
        serviceCollection.AddSingleton<RegistrationConsumer<TKey, TValue, THandler>>(sp =>
        {
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            var handler = sp.GetService<THandler>()!;
            if (globalSettings is null)
            {
                throw new InvalidOperationException("Глобальная конфигурация не добавлена");
            }
            config.Handler = handler;
            var consumer = new ConsumerBuilder<TKey, TValue>(
                    new ConsumerConfig().FromConsumerSettings(config, globalSettings))
                .SetKeyDeserializer(new JsonDeserializer<TKey>())
                .SetValueDeserializer(new JsonDeserializer<TValue>())
                .Build();
            
            return new RegistrationConsumer<TKey, TValue, THandler>
            {
                Settings = config,
                Consumer = consumer
            };
        });
        
        serviceCollection.AddHostedService<TransactionalConsumerService<TKey, TValue, THandler>>(sp =>
        {
            var logger = sp.GetService<ILogger<TransactionalConsumerService<TKey, TValue, THandler>>>()!;
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            var producer = sp.GetService<IProducer<byte[]?, byte[]>>();
            var registrationConsumer = sp.GetService<RegistrationConsumer<TKey, TValue, THandler>>()!;
            
            if (producer is null || globalSettings is null)
            {
                throw new InvalidOperationException($"Не добавлена конфигурация продюсера {nameof(AddProducerGeneral)}" +
                                                    $" или глобальная {nameof(AddKafka)}");
            }

            return new TransactionalConsumerService<TKey, TValue, THandler>(
                globalSettings, registrationConsumer, sp, producer, logger);
        });

        return serviceCollection;
    }
    
    public static IServiceCollection AddConsumer<TValue, THandler>(
        this IServiceCollection serviceCollection,
        Action<ConsumerSettings<THandler>> configure) where THandler: class, IMessageHandler<TValue>
    {
        var config = new ConsumerSettings<THandler>();
        configure(config);
        serviceCollection.AddSingleton<THandler>();
        
        serviceCollection.AddSingleton<RegistrationConsumer<Null, TValue, THandler>>(sp =>
        {
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            var handler = sp.GetService<THandler>()!;
            if (globalSettings is null)
            {
                throw new InvalidOperationException("Глобальная конфигурация не добавлена");
            }
            config.Handler = handler;
            var consumer = new ConsumerBuilder<Null, TValue>(
                    new ConsumerConfig().FromConsumerSettings(config, globalSettings))
                .SetValueDeserializer(new JsonDeserializer<TValue>())
                .Build();
            
            return new RegistrationConsumer<Null, TValue, THandler>
            {
                Settings = config,
                Consumer = consumer
            };
        });
        serviceCollection.AddHostedService<ConsumerService<TValue, THandler>>(sp =>
        {
            var logger = sp.GetService<ILogger<ConsumerService<TValue, THandler>>>()!;
            var registrationConsumer = sp.GetService<RegistrationConsumer<Null, TValue, THandler>>()!;
            return new ConsumerService<TValue, THandler>(
                registrationConsumer, logger);
        });
        
        return serviceCollection;
    }
    
    public static IServiceCollection AddTransactionConsumer<TValue, THandler>(
        this IServiceCollection serviceCollection,
        Action<ConsumerSettings<THandler>> configure) 
        where THandler: class, ITransactionMessageHandler<TValue>
    {
        var config = new ConsumerSettings<THandler>();
        configure(config);
        serviceCollection.AddSingleton<THandler>();
        
        serviceCollection.AddSingleton<RegistrationConsumer<Null, TValue, THandler>>(sp =>
        {
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            var handler = sp.GetService<THandler>()!;
            if (globalSettings is null)
            {
                throw new InvalidOperationException("Глобальная конфигурация не добавлена");
            }
            config.Handler = handler;
            var consumer = new ConsumerBuilder<Null, TValue>(
                    new ConsumerConfig().FromConsumerSettings(config, globalSettings))
                .SetValueDeserializer(new JsonDeserializer<TValue>())
                .Build();
            
            return new RegistrationConsumer<Null, TValue, THandler>
            {
                Settings = config,
                Consumer = consumer
            };
        });
        
        serviceCollection.AddHostedService<TransactionalConsumerService<TValue, THandler>>(sp =>
        {
            var logger = sp.GetService<ILogger<TransactionalConsumerService<TValue, THandler>>>()!;
            var globalSettings = sp.GetService<KafkaGlobalSettings>();
            var producer = sp.GetService<IProducer<byte[]?, byte[]>>();
            var registrationConsumer = sp.GetService<RegistrationConsumer<Null, TValue, THandler>>()!;
            
            if (producer is null || globalSettings is null)
            {
                throw new InvalidOperationException($"Не добавлена конфигурация продюсера {nameof(AddProducerGeneral)}" +
                                                    $" или глобальная {nameof(AddKafka)}");
            }

            return new TransactionalConsumerService<TValue, THandler>(
                globalSettings, registrationConsumer, sp, producer, logger);
        });

        return serviceCollection;
    }
}