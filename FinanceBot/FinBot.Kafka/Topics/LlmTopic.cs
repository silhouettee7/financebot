using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

public class LlmTopic: ITopic
{
    public string TopicName => "llm";
}