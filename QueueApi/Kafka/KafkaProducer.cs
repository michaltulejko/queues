using Confluent.Kafka;
using QueueApi.Kafka.Interfaces;

namespace QueueApi.Kafka
{
    public class KafkaProducer(
        IProducer<string, long> producer,
        ILogger<KafkaProducer>? logger = null)
        : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, long> _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public async Task ProduceAsync(string topic, Guid key, long timestamp, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(KafkaProducer));

            var message = new Message<string, long>
            {
                Key = key.ToString(),
                Value = timestamp
            };

            // Use SemaphoreSlim for better async handling in high-concurrency scenarios
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
                logger?.LogInformation("Kafka message sent with key: {Key}, value: {Value}, partition: {Partition}, offset: {Offset}",
                    deliveryResult.Key, deliveryResult.Value, deliveryResult.Partition, deliveryResult.Offset);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _producer.Dispose();
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}