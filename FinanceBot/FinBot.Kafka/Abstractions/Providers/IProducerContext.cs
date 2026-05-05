namespace FinBot.Kafka.Abstractions.Providers;

public interface IProducerContext: IAsyncProducerProvider
{
    void BeginTransaction();
    void CommitTransactionAsync();
    void AbortTransaction();
}