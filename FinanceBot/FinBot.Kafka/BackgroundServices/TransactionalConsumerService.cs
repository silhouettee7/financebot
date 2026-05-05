using Confluent.Kafka;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Impl.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.BackgroundServices;

public class TransactionalConsumerService<TKey, TValue, THandler>(
    THandler handler,
    KafkaGlobalSettings kafkaGlobalSettings,
    RegistrationConsumer<TKey, TValue> registrationConsumer,
    IServiceProvider serviceProvider,
    IProducer<byte[]?, byte[]> producer,
    ILogger<TransactionalConsumerService<TKey, TValue, THandler>> logger)
    : BackgroundService where THandler : ITransactionMessageHandler<TKey, TValue>
{
    private readonly IConsumer<TKey, TValue> _consumer = registrationConsumer.Consumer;
    private readonly ConsumerSettings _settings = registrationConsumer.Settings;
    private THandler _handler = handler;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_settings.Topics.Select(t => t.TopicName));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                
                var nextOffset = new TopicPartitionOffset(
                    consumeResult.TopicPartition, 
                    consumeResult.Offset + 1
                );
                var offsetsToCommit = new List<TopicPartitionOffset> { nextOffset };
                
                var context = new ConsumeProduceContext(
                    kafkaGlobalSettings, 
                    serviceProvider, 
                    producer,
                    _consumer.ConsumerGroupMetadata, 
                    offsetsToCommit);
                
                var retryCount = 0;
                var success = false;
                
                while (!success && retryCount < _settings.MaxRetryCount && !stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _handler.HandleAsync(
                            consumeResult.Message.Key, 
                            consumeResult.Message.Value, 
                            context, 
                            stoppingToken);
                        success = true;
                    }
                    catch (Exception ex) when (retryCount < _settings.MaxRetryCount - 1)
                    {
                        retryCount++;
                        logger.LogWarning(ex, "Ошибка обработки сообщения, ретрай: {RetryCount}/{MaxRetryCount}", retryCount, _settings.MaxRetryCount);
                        await Task.Delay(_settings.RetryDelay, stoppingToken);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка обработки сообщения после {MaxRetryCount} ретраев", _settings.MaxRetryCount);
                            
                        if (_settings.EnableDeadLetterQueue)
                        {
                            await SendToDeadLetterQueue(consumeResult, ex, stoppingToken);
                        }
                        throw;
                    }
                    try
                    {
                        await context.ExecuteTransactionAsync(stoppingToken);
                    
                        if (!_settings.EnableAutoCommit && success)
                        {
                            _consumer.Commit(consumeResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        context.AbortTransaction();
                        
                        logger.LogError(ex, "Ошибка при транзакционном получении и отправке сообщения");

                        throw;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Произошла ошибка при обработке сообщения");
                await Task.Delay(_settings.ErrorMessageHandleDelay, stoppingToken);
            }
        }
        
        _consumer.Close();
    }
    
    private async Task SendToDeadLetterQueue(ConsumeResult<TKey, TValue> consumeResult, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogInformation("Отправка в DLQ: {Exception}", exception.Message);
        await Task.CompletedTask;
    }
    
    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}