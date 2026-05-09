using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Docker.DotNet.Models;
using FinBot.Kafka.Extensions;
using FinBot.Kafka.Tests.TestEnvironment;
using FinBot.Kafka.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Kafka;

namespace FinBot.Kafka.Tests;

public class ConsumerTests
{
    private readonly TestTopic _topic = new ();
    private readonly TopicResponse _topicResponse = new ();
    private readonly string _groupId = "1";
    private readonly string _responseGroupId = "2";

    [Fact]
    public async Task ConsumerBackgroundService_ShouldHandleMessage()
    {
        var kafkaContainer = new KafkaBuilder("confluentinc/cp-kafka:7.4.0")
            .Build();
        
        await kafkaContainer.StartAsync();

        var bootstrapAddress = kafkaContainer.GetBootstrapAddress();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        
        services.AddKafka(config => config.BootstrapServers = bootstrapAddress);
        services.AddProducerGeneral();
        services.AddConsumer<TestMessage,TestMessageHandler>(config =>
        {
            config.GroupId = _groupId;
            config.Topics.Add(_topic);
        });
        
        var host = builder.Build();
        await host.StartAsync();
        var serviceProvider = host.Services;
        
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = bootstrapAddress }
        ).Build();
        await adminClient.CreateTopicsAsync([
            new TopicSpecification
            {
                Name = _topic.TopicName,
                NumPartitions = 1,
                ReplicationFactor = 1
            }
        ]);

        var producer = new ProducerBuilder<Null, TestMessage>(new ProducerConfig
        {
            BootstrapServers = bootstrapAddress
        })
            .SetValueSerializer(new JsonSerializer<TestMessage>())
            .Build();
        
        var message = new TestMessage { Body = "Test" };
        await producer.ProduceAsync(_topic.TopicName, 
            new Message<Null, TestMessage> {Value = message});
        
        await Task.Delay(3000);
        var handler = serviceProvider.GetRequiredService<TestMessageHandler>();
        
        await kafkaContainer.DisposeAsync();
        
        Assert.NotNull(handler.Message);
        Assert.Equal(message.Body, handler.Message?.Body);
    }
    
    [Fact]
    public async Task TransactionalConsumerBackgroundService_ShouldHandleMessageAtomically()
    {
        var kafkaContainer = new KafkaBuilder("confluentinc/cp-kafka:7.4.0")
            .Build();
        
        await kafkaContainer.StartAsync();

        var bootstrapAddress = kafkaContainer.GetBootstrapAddress();
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;
        
        services.AddKafka(config => config.BootstrapServers = bootstrapAddress);
        services.AddProducerGeneral();
        services.AddTransactionConsumer<TestMessage,TestTransactionalMessageHandler>(config =>
        {
            config.GroupId = _groupId;
            config.Topics.Add(_topic);
        });
        services.AddProducer<TestMessage,TopicResponse>(config =>
        {
            config.Topic = _topicResponse;
        });
        
        var host = builder.Build();
        await host.StartAsync();
        
        using var adminClient = new AdminClientBuilder(
            new AdminClientConfig { BootstrapServers = bootstrapAddress }
        ).Build();
        await adminClient.CreateTopicsAsync([
            new TopicSpecification
            {
                Name = _topic.TopicName,
                NumPartitions = 1,
                ReplicationFactor = 1
            }
        ]);
        await adminClient.CreateTopicsAsync([
            new TopicSpecification
            {
                Name = _topicResponse.TopicName,
                NumPartitions = 1,
                ReplicationFactor = 1
            }
        ]);

        var producer = new ProducerBuilder<Null, TestMessage>(new ProducerConfig
            {
                BootstrapServers = bootstrapAddress
            })
            .SetValueSerializer(new JsonSerializer<TestMessage>())
            .Build();
        var consumer = new ConsumerBuilder<Null,TestMessage>(
                new ConsumerConfig
                {
                    BootstrapServers = bootstrapAddress,
                    GroupId = _responseGroupId,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                })
            .SetValueDeserializer(new JsonDeserializer<TestMessage>())
            .Build();
        consumer.Subscribe([_topicResponse.TopicName]);
        
        var message = new TestMessage { Body = "Test" };
        await producer.ProduceAsync(_topic.TopicName, 
            new Message<Null, TestMessage> {Value = message});
        
        var receivedMessages = new List<TestMessage>();
        var attempts = 3;
        while (attempts-- > 0)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(3));
            if (consumeResult != null)
            {
                receivedMessages.Add(consumeResult.Message.Value);
                consumer.Commit(consumeResult);
            }
        }
        
        await kafkaContainer.DisposeAsync();
        
        Assert.Single(receivedMessages);
        Assert.Equal(message.Body.ToUpper(), receivedMessages.FirstOrDefault()?.Body);
    }
}