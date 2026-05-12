using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

public class ApiBankTopic: ITopic
{
    public string TopicName => "api-bank";
}