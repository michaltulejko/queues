using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitWorker;

public class RabbitWorker(
    IConnection rabbitConnection,
    ILogger<RabbitWorker> logger)
    : BackgroundService
{
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
            var message = Encoding.UTF8.GetString(body);
            var eventTime = long.Parse(message);
            // Compare event creation time with current time (UTC)
            var eventDateTime = DateTimeOffset.FromUnixTimeSeconds(eventTime).DateTime;
            var delay = now - eventDateTime;
            logger.LogInformation("Delay between event creation and processing: {Delay}", delay);

            // Acknowledge the message after processing.
            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        // Begin consuming messages.
        _channel.BasicConsume(queue: "testQ", autoAck: false, consumer: consumer);

        return base.StartAsync(cancellationToken);
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
