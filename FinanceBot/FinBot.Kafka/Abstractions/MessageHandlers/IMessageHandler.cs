namespace FinBot.Kafka.Abstractions.MessageHandlers;

public interface IMessageHandler<in TKey, in TValue>
{
    Task HandleAsync(TKey key,TValue message, CancellationToken cancellationToken = default);
}

public interface IMessageHandler<in TValue>
{
    Task HandleAsync(TValue message, CancellationToken cancellationToken = default);
}