namespace FinBot.Kafka.Abstractions.Producers;

public interface IProducer<in TValue>
{
    public string Topic { get; }
    void Produce(TValue value, CancellationToken cancellationToken = default);
}

public interface IProducer<in TKey, in TValue>
{
    public string Topic { get; }
    void Produce(TKey key, TValue value, CancellationToken cancellationToken = default);
}