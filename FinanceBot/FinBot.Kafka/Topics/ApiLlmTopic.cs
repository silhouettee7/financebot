using FinBot.Kafka.Abstractions;

namespace FinBot.Kafka.Topics;

public class ApiLlmTopic: ITopic
{
    public string TopicName => "api-llm";
}