using Confluent.Kafka;
using QueueApi.Kafka.Interfaces;

namespace QueueApi.Kafka
{
    public class KafkaProducer(
        IProducer<string, long> producer) : IKafkaProducer
    {
        public async Task ProduceAsync(string topic, Guid key, long timestamp, CancellationToken cancellationToken = default)
        {
            await producer.ProduceAsync(topic, new Message<string, long>
            {
                Key = key.ToString(),
                Value = timestamp
            }, cancellationToken);
        }
    }
}
