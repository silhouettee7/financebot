namespace FinBot.Kafka.Abstractions.Producers;

public interface IAsyncProducer<in TKey, in TValue>: IProducer<TKey, TValue>
{
    Task ProduceAsync(TKey key, TValue value, CancellationToken cancellationToken = default);
}

public interface IAsyncProducer<in TValue>: IProducer<TValue>
{
    Task ProduceAsync(TValue value, CancellationToken cancellationToken = default);
}