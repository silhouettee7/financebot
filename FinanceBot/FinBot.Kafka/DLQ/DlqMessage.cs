
namespace FinBot.Kafka.DLQ;

public record DlqMessage(
    string ExceptionMessage, 
    long? Offset,
    string? TopicName,
    int? Partition,
    string Key,
    string Value);