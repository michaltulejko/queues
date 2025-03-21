using Common;
using Confluent.Kafka;

namespace KafkaWorker
{
    public class KafkaWorker(
        IConsumer<string, MessageTime> kafkaConsumer,
        MetricsCollector metricsCollector,
        ILogger<KafkaWorker> logger) : BackgroundService
    {
        private const string QueueName = "Kafka";

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = kafkaConsumer;
            // Subscribe to the target topic
            consumer.Subscribe("test");
            logger.LogInformation("Subscribed to topic: test");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Consume messages synchronously (the consumer is not thread‑safe)
                        var result = consumer.Consume(stoppingToken);

                        // Process the message immediately, as fast as possible
                        ProcessMessage(result.Message);
                    }
                    catch (ConsumeException ex)
                    {
                        logger.LogError(ex, "Error while consuming message: {Reason}", ex.Error.Reason);
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful shutdown requested
                        break;
                    }
                }
            }
            finally
            {
                consumer.Close();
            }

            return Task.CompletedTask;
        }

        private void ProcessMessage(MessageTime messageTimeEvent)
        {
            logger.LogInformation("Received event created at: {EventTime}", messageTimeEvent.EntryTimeStamp);

            // Compare event creation time with current time (UTC)
            var now = DateTime.UtcNow;
            var eventDateTime = DateTimeOffset.FromUnixTimeSeconds(eventTime).DateTime;
            var delay = now - eventDateTime;
            logger.LogInformation("Delay between event creation and processing: {Delay}", delay);

            metricsCollector.Enqueue(new DelayMeasurement(eventTime, delay, QueueName));
        }
    }
}
