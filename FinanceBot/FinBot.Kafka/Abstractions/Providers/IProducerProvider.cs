using FinBot.Kafka.Abstractions.Producers;

namespace FinBot.Kafka.Abstractions.Providers;

public interface IProducerProvider
{
    IProducer<TKey, TValue> GetProducer<TKey, TValue, TTopic>() where TTopic : ITopic;
    IProducer<TValue> GetProducer<TValue, TTopic>() where TTopic : ITopic;
}