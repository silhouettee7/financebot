using System.Text.Json;
using Confluent.Kafka;
using FinBot.Kafka.Abstractions.Producers;
using FinBot.Kafka.Abstractions.Providers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.DLQ;
using FinBot.Kafka.Internal.DI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.BackgroundServices.Base;

internal abstract class ConsumerServiceBase<TKey, TValue>(
    RegistrationConsumer<TKey, TValue> registrationConsumer,
    IAsyncProducerProvider producerProvider,
    ILogger<ConsumerServiceBase<TKey, TValue>> logger)
    : BackgroundService
{
    private readonly IAsyncProducer<DlqMessage> _dlqProducer =
        producerProvider.GetProducer<DlqMessage, DlqTopic>();
    
    protected readonly IConsumer<TKey, TValue> Consumer = registrationConsumer.Consumer;
    protected readonly ConsumerSettings Settings = registrationConsumer.Settings;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Consumer.Subscribe(Settings.Topics.Select(t => t.TopicName));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = Consumer.Consume(stoppingToken);
                
                var retryCount = 0;
                var success = false;
                
                while (!success && retryCount < Settings.MaxRetryCount && !stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await HandleMessageAsync(consumeResult, stoppingToken);
                        success = true;
                    }
                    catch (Exception ex) when (retryCount < Settings.MaxRetryCount - 1)
                    {
                        retryCount++;
                        logger.LogWarning(ex, "Ошибка обработки сообщения, ретрай: {RetryCount}/{MaxRetryCount}", retryCount, Settings.MaxRetryCount);
                        await Task.Delay(Settings.RetryDelay, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка обработки сообщения после {MaxRetryCount} ретраев", Settings.MaxRetryCount);
                            
                        if (Settings.EnableDeadLetterQueue)
                        {
                            await SendToDeadLetterQueue(consumeResult, ex, stoppingToken);
                        }
                        throw;
                    }
                }
                await ExecuteAfterMessageHandleAsync(success,consumeResult, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Произошла ошибка при обработке сообщения");
                await Task.Delay(Settings.ErrorMessageHandleDelay, stoppingToken);
            }
        }
        
        Consumer.Close();
    }
    
    public override void Dispose()
    {
        Consumer.Dispose();
        base.Dispose();
    }
    
    protected abstract Task HandleMessageAsync(
        ConsumeResult<TKey,TValue> consumeResult, 
        CancellationToken cancellationToken);
    
    protected abstract Task ExecuteAfterMessageHandleAsync(
        bool success,
        ConsumeResult<TKey,TValue> consumeResult, 
        CancellationToken cancellationToken);
    
    private async Task SendToDeadLetterQueue(ConsumeResult<TKey, TValue>? consumeResult, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogInformation("Отправка в DLQ: {Exception}", exception.Message);
        var key = "";
        var value = "";
        if (consumeResult != null)
        {
            key = JsonSerializer.Serialize(consumeResult.Message.Key);
            value = JsonSerializer.Serialize(consumeResult.Message.Value);
        }
        var message = new DlqMessage(
            exception.Message, consumeResult?.Offset.Value, 
            consumeResult?.Topic, consumeResult?.Partition.Value, key, value);
        await _dlqProducer.ProduceAsync(message, cancellationToken);
        await Task.CompletedTask;
    }
}