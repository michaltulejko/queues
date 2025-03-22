using Common;
using Confluent.Kafka;
using QueueApi.Kafka.Interfaces;
using System.Text.Json;

namespace QueueApi.Kafka
{
    public class KafkaProducer(
        IProducer<string, string> producer,
        ILogger<KafkaProducer>? logger = null)
        : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public async Task ProduceAsync(string topic, Guid key, long entryTimestamp, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(KafkaProducer));

            var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            var result = new ProcessingMeasurements
            {
                EventCreatedTimeStamp = entryTimestamp,
                ProcessingTimeStamp = unixTimeSeconds
            };

            var message = new Message<string, string>
            {
                Key = key.ToString(),
                Value = JsonSerializer.Serialize(result)
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
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}