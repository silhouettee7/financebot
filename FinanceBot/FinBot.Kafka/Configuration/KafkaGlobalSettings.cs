namespace FinBot.Kafka.Configuration;

public class KafkaGlobalSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan TransactionInitDelay { get; set; } = TimeSpan.FromSeconds(5);
}