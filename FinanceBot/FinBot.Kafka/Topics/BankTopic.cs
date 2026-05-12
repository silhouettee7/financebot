using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

public class BankTopic: ITopic
{
    public string TopicName => "bank";
}