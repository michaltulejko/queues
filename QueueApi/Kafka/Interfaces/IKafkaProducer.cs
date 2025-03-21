namespace QueueApi.Kafka.Interfaces
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(string topic, Guid key, long entryTimestamp, CancellationToken cancellationToken = default);
    }
}
