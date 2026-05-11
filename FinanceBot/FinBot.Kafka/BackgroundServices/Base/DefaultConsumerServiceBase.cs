using Confluent.Kafka;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Internal.DI;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.BackgroundServices.Base;

internal abstract class DefaultConsumerServiceBase<TKey, TValue>(
    RegistrationConsumer<TKey, TValue> registrationConsumer,
    IAsyncProducerProvider producerProvider,
    ILogger<DefaultConsumerServiceBase<TKey, TValue>> logger)
    : ConsumerServiceBase<TKey, TValue>(registrationConsumer, producerProvider, logger)
{
    protected override Task ExecuteAfterMessageHandleAsync(
        bool success, 
        ConsumeResult<TKey, TValue> consumeResult, 
        CancellationToken cancellationToken)
    {
        if (!Settings.EnableAutoCommit && success)
        {
            Consumer.Commit(consumeResult);
        }
        return Task.CompletedTask;
    }
}