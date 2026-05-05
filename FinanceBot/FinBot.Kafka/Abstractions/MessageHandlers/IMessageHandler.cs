namespace FinBot.Kafka.Abstractions.MessageHandlers;

public interface IMessageHandler<in TKey, in TValue>
{
    Task HandleAsync(TKey key,TValue message, CancellationToken cancellationToken = default);
}