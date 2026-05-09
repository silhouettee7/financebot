using FinBot.Kafka.Abstractions.Providers;

namespace FinBot.Kafka.Abstractions.MessageHandlers;

public interface ITransactionMessageHandler<in TKey,in TValue>
{
    Task HandleAsync(TKey key, TValue message, IConsumeProduceContext context,
        CancellationToken cancellationToken = default);
}

public interface ITransactionMessageHandler<in TValue>
{
    Task HandleAsync(TValue message, IConsumeProduceContext context,
        CancellationToken cancellationToken = default);
}