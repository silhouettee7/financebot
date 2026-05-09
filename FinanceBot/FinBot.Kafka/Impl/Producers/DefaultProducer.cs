using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Abstractions.Producers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Utils;

namespace FinBot.Kafka.Impl.Producers;

internal class DefaultProducer<TKey, TValue, TTopic>(
    ProducerSettings<TKey,TValue, TTopic> config,
    Confluent.Kafka.IProducer<byte[]?,byte[]> producer)
    : IAsyncProducer<TKey, TValue> where TTopic : ITopic
{
    private ISerializer<TValue> ValueSerializer => config.ValueSerializer;
    private ISerializer<TKey> KeySerializer => config.KeySerializer;
    public string Topic => config.Topic.TopicName;
    
    public async Task ProduceAsync(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        await producer.ProduceAsync(Topic, MessageHelper
            .GetDeserializedMessage(Topic, key, value, KeySerializer, ValueSerializer), 
            cancellationToken);
    }

    public void Produce(TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        producer.Produce(Topic, MessageHelper
            .GetDeserializedMessage(Topic, key, value, KeySerializer, ValueSerializer));
    }
}

internal class DefaultProducer<TValue, TTopic>(
    ProducerSettings<Null,TValue, TTopic> config,
    Confluent.Kafka.IProducer<byte[]?,byte[]> producer)
    : IAsyncProducer<TValue> where TTopic : ITopic
{
    private ISerializer<TValue> ValueSerializer => config.ValueSerializer; 
    public string Topic => config.Topic.TopicName;
    
    
    public async Task ProduceAsync(TValue value, CancellationToken cancellationToken = default)
    {
        await producer.ProduceAsync(Topic, MessageHelper
            .GetDeserializedMessage<object, TValue>(
                Topic, null, value, null, ValueSerializer), cancellationToken);
    }

    public void Produce(TValue value)
    {
        producer.Produce(Topic, MessageHelper
            .GetDeserializedMessage<object, TValue>(
                Topic, null, value, null, ValueSerializer));
    }
}