using Confluent.Kafka;
using FinBot.Kafka.Configuration;

namespace FinBot.Kafka;

public class RegistrationConsumer<TKey, TValue, THandler>
{
    public required IConsumer<TKey,TValue> Consumer { get; set; }
    public required ConsumerSettings<THandler> Settings { get; set; }
}