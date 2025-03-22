using System.Text.Json;
using Common;
using Confluent.Kafka;

namespace KafkaWorker
{
    public class KafkaWorker(
        IConsumer<string, string> kafkaConsumer,
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
                        var measurements = JsonSerializer.Deserialize<ProcessingMeasurements>(result.Message.Value);

                        // Process the message immediately, as fast as possible
                        ProcessMessage(measurements ?? default);
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

        private void ProcessMessage(ProcessingMeasurements? processingMeasurementsEvent)
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
    }
}
