using FinBot.Kafka.Abstractions.Providers;

namespace FinBot.Kafka.Abstractions.MessageHandlers;

public interface ITransactionMessageHandler<in TKey,in TValue>
{
    Task HandleAsync(TKey key, TValue message, IConsumeProduceContext context,
        CancellationToken cancellationToken = default);
}