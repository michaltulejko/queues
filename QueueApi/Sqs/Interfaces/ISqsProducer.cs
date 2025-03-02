namespace QueueApi.Sqs.Interfaces
{
    public interface ISqsProducer
    {
        Task ProduceAsync(string topic, Guid key, long timestamp, CancellationToken cancellationToken = default);
    }
}
