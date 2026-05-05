using Confluent.Kafka;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.Kafka.Configuration;
using FinBot.Kafka.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinBot.Kafka.BackgroundServices;

public class ConsumerService<TKey, TValue, THandler>(
    RegistrationConsumer<TKey, TValue> registrationConsumer,
    THandler handler,
    ILogger<ConsumerService<TKey, TValue, THandler>> logger)
    : BackgroundService where THandler : IMessageHandler<TKey, TValue>
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
                var retryCount = 0;
                var success = false;
                
                while (!success && retryCount < _settings.MaxRetryCount && !stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await _handler.HandleAsync(consumeResult.Message.Key, consumeResult.Message.Value, stoppingToken);
                        success = true;
                    }
                    catch (Exception ex) when (retryCount < _settings.MaxRetryCount - 1)
                    {
                        retryCount++;
                        logger.LogWarning(ex, "Ошибка обработки сообщения, ретрай: {RetryCount}/{MaxRetryCount}", retryCount, _settings.MaxRetryCount);
                        await Task.Delay(_settings.RetryDelay, stoppingToken);
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
                }
                
                if (!_settings.EnableAutoCommit && success)
                {
                    _consumer.Commit(consumeResult);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обработке сообщения");
                await Task.Delay(_settings.ErrorMessageHandleDelay, stoppingToken);
            }
        }
        
        _consumer.Close();
    }
    
    private async Task SendToDeadLetterQueue(ConsumeResult<TKey, TValue> consumeResult, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogInformation("Отправка сообщения в DLQ: {Exception}", exception.Message);
        await Task.CompletedTask;
    }
    
    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}