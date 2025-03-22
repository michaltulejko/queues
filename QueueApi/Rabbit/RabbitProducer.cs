using Common;
using QueueApi.Rabbit.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace QueueApi.Rabbit
{
    public class RabbitProducer : IRabbitProducer, IDisposable
    {
        private readonly ILogger<RabbitProducer>? _logger;
        private readonly IModel _channel;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public RabbitProducer(
            IConnection rabbitConnection,
            ILogger<RabbitProducer>? logger = null)
        {
            _logger = logger;
            var rabbitConnection1 = rabbitConnection ?? throw new ArgumentNullException(nameof(rabbitConnection));
            _channel = rabbitConnection1.CreateModel();
        }

        public async Task ProduceAsync(string topic, Guid key, long entryTimestamp, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitProducer));

            var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            var processingMeasurements = new ProcessingMeasurements
            {
                EventCreatedTimeStamp = entryTimestamp,
                ProcessingTimeStamp = unixTimeSeconds
            };

            var json = JsonSerializer.Serialize(processingMeasurements);
            var body = Encoding.UTF8.GetBytes(json);

            // Use SemaphoreSlim for async thread safety
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                _channel.BasicPublish(
                    exchange: "testEx",
                    routingKey: topic,
                    basicProperties: null,
                    body: body);
                _logger?.LogInformation("Rabbit message sent with key: {Key}, value: {Value}",
                    key, processingMeasurements);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _channel?.Close();
            _channel?.Dispose();
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}
