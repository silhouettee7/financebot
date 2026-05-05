using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Configuration;

namespace FinBot.Kafka;

internal class RegistrationProducer<TKey, TValue,TTopic> where TTopic : ITopic
{
    public required ProducerSettings<TKey,TValue,TTopic> Settings { get; set; }
    public required IProducer<byte[]?, byte[]> Producer { get; set; }
}