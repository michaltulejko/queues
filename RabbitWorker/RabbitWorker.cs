using Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace RabbitWorker;

public class RabbitWorker(
    IConnection rabbitConnection,
    MetricsCollector metricsCollector,
    ILogger<RabbitWorker> logger)
    : BackgroundService
{
    private const string QueueName = "Rabbit";
    private readonly IModel _channel = rabbitConnection.CreateModel();

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _channel.QueueBind(
            queue: "testQ",
            exchange: "testEx",
            routingKey: "test"
        );

        logger.LogInformation("RabbitMQ Consumer connected. Exchange '{Exchange}' and Queue '{Queue}' declared.", "testEx", "testQ");

        // Set up the consumer.
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var now = DateTime.UtcNow;
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            var processingMeasurements = JsonSerializer.Deserialize<ProcessingMeasurements>(json);

            ProcessMessage(processingMeasurements);

            // Acknowledge the message after processing.
            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        // Begin consuming messages.
        _channel.BasicConsume(queue: "testQ", autoAck: false, consumer: consumer);

        return base.StartAsync(cancellationToken);
    }

    private void ProcessMessage(ProcessingMeasurements processingMeasurementsEvent)
    {
        var processingTime = processingMeasurementsEvent.ProcessingTimeStamp;
        logger.LogInformation("Received event created at: {EventTime}", processingMeasurementsEvent.EventCreatedTimeStamp);
        var creationTime = processingMeasurementsEvent.EventCreatedTimeStamp;

        // Compare event creation time with current time (UTC)
        var now = DateTime.UtcNow;
        var processingDateTime = DateTimeOffset.FromUnixTimeSeconds(processingTime).DateTime;
        var processingDelay = now - processingDateTime;
        var creationDateTime = DateTimeOffset.FromUnixTimeSeconds(creationTime).DateTime;
        var creationDelay = now - creationDateTime;
        logger.LogInformation("Delay between event creation and processing: {Delay}", processingDelay);

        metricsCollector.Enqueue(new DelayMeasurement(creationTime, creationDelay, processingTime, processingDelay, QueueName));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The consumer's event handler processes messages,
        // so we simply wait here until a cancellation is requested.
        var tcs = new TaskCompletionSource<object?>();
        stoppingToken.Register(() => tcs.TrySetResult(null));
        return tcs.Task;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        logger.LogInformation("RabbitMQ channel closed.");
        return base.StopAsync(cancellationToken);
    }
}
