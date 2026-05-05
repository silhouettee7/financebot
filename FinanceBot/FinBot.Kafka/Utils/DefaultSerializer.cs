using System.Text.Json;
using Confluent.Kafka;

namespace FinBot.Kafka.Utils;

public class DefaultSerializer<T>: ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}