using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.DLQ;

public class DlqTopic: ITopic
{
    public string TopicName => "dlq";
}