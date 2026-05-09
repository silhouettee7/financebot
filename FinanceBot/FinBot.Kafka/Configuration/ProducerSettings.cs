using Confluent.Kafka;
using FinBot.Kafka.Abstractions;
using FinBot.Kafka.Utils;

namespace FinBot.Kafka.Configuration;

public class ProducerSettings<TKey, TValue, TTopic> where TTopic : ITopic
{
    public ProducerSettingsGeneral GeneralSettings { get; internal set; } = new();
    public TTopic Topic { get; set; } = default!;
    public ISerializer<TKey> KeySerializer { get; set; } = new JsonSerializer<TKey>();
    public ISerializer<TValue> ValueSerializer { get; set; } = new JsonSerializer<TValue>();
}