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
    private readonly string _groupId = "1";

    [Fact]
    public async Task ConsumerBackgroundService_Should_Handle_Message()
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
}