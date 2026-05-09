using Confluent.Kafka;
using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Configuration;

public class ConsumerSettings<THandler>
{
    public string GroupId { get; set; } = Guid.NewGuid().ToString();
    public bool EnableAutoCommit { get; set; } = false;
    public int AutoCommitIntervalMs { get; set; } = 5000;
    public bool EnableAutoOffsetStore { get; set; } = false;
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
    public List<ITopic> Topics { get; set; } = new();
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan ErrorMessageHandleDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableDeadLetterQueue { get; set; } = true;
    public string DeadLetterTopicSuffix { get; set; } = "-dlq";
    public THandler Handler { get; internal set; }
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
}