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
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
           
            return new ProducerBuilder<byte[]?, byte[]>(
                    config.FromProducerSettingsGeneral(settings, globalSettings))
                .Build();
        });
        serviceCollection.AddSingleton<IAsyncProducerProvider,DefaultProducerProvider>();
        serviceCollection.AddScoped<IProducerContext, ProducerContext>(sp =>
        {
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();

            config = config.FromProducerSettingsGeneral(settings, globalSettings);
            config.TransactionalId = Guid.NewGuid().ToString();
            var producer = new ProducerBuilder<byte[]?, byte[]>(config)
                .Build();
            //TODO внедрить пул продюсеров в случае если понадобится транзакционный контекст
            //TODO сейчас на каждый вызов будет тратиться минимум 5 секунд, чтобы инициализировать продюсера
            producer.InitTransactions(TimeSpan.FromSeconds(5));
            return new ProducerContext(producer, sp);
        });
        
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
                var producer = sp.GetRequiredService<IProducer<byte[]?, byte[]>>();
                var generalSettings = sp.GetRequiredService<ProducerSettingsGeneral>();
                var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
                
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
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
            var handler = sp.GetRequiredService<THandler>();
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
            var logger = sp.GetRequiredService<ILogger<ConsumerService<TKey, TValue, THandler>>>();
            var registrationConsumer = sp.GetRequiredService<RegistrationConsumer<TKey, TValue, THandler>>();
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
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
            var handler = sp.GetRequiredService<THandler>();
           
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
            var logger = sp.GetRequiredService<ILogger<TransactionalConsumerService<TKey, TValue, THandler>>>();
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
            var registrationConsumer = sp.GetRequiredService<RegistrationConsumer<TKey, TValue, THandler>>();

            var settings = sp.GetRequiredService<ProducerSettingsGeneral>();
            var producerConfig = new ProducerConfig()
                .FromProducerSettingsGeneral(settings, globalSettings);
            producerConfig.TransactionalId = Guid.NewGuid().ToString();
            var producer = new ProducerBuilder<byte[]?, byte[]>(producerConfig)
                .Build();
            producer.InitTransactions(TimeSpan.FromSeconds(5));
            
            return new TransactionalConsumerService<TKey, TValue, THandler>(
                globalSettings, registrationConsumer, sp, producer,logger);
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
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
            var handler = sp.GetRequiredService<THandler>();
            
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
            var logger = sp.GetRequiredService<ILogger<ConsumerService<TValue, THandler>>>();
            var registrationConsumer = sp.GetRequiredService<RegistrationConsumer<Null, TValue, THandler>>();
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
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
            var handler = sp.GetRequiredService<THandler>();
           
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
            var logger = sp.GetRequiredService<ILogger<TransactionalConsumerService<TValue, THandler>>>();
            var globalSettings = sp.GetRequiredService<KafkaGlobalSettings>();
            var registrationConsumer = sp.GetRequiredService<RegistrationConsumer<Null, TValue, THandler>>();
            
            var settings = sp.GetRequiredService<ProducerSettingsGeneral>();
            var producerConfig = new ProducerConfig()
                .FromProducerSettingsGeneral(settings, globalSettings);
            producerConfig.TransactionalId = Guid.NewGuid().ToString();
            var producer = new ProducerBuilder<byte[]?, byte[]>(producerConfig)
                .Build();
            producer.InitTransactions(TimeSpan.FromSeconds(5));
            
            return new TransactionalConsumerService<TValue, THandler>(
                globalSettings, registrationConsumer, sp, producer,logger);
        });

        return serviceCollection;
    }
}