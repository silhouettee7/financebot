using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

public class ApiExcelTopic: ITopic
{
    public string TopicName => "api-excel";
}