using FinBot.Kafka.Abstractions.Producers;

namespace FinBot.Kafka.Abstractions.Providers;

public interface IAsyncProducerProvider
{
    IAsyncProducer<TKey, TValue> GetProducer<TKey, TValue, TTopic>() where TTopic : ITopic;
    IAsyncProducer<TValue> GetProducer<TValue, TTopic>() where TTopic : ITopic;
}