namespace QueueApi.Rabbit.Interfaces
{
    public interface IRabbitProducer
    {
        Task ProduceAsync(string topic, Guid key, long timestamp, CancellationToken cancellationToken = default);
    }
}
