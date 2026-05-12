using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

public class ExcelTopic: ITopic
{
    public string TopicName => "excel";
}