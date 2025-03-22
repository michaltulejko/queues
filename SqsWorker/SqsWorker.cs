using Amazon.SQS;
using Amazon.SQS.Model;
using Common;
using System.Text.Json;

namespace SqsWorker;

public class SqsWorker(
    IAmazonSQS sqs,
    MetricsCollector metricsCollector,
    ILogger<SqsWorker> logger) : BackgroundService
{
    // Read the SQS queue name from configuration.
    private const string QueueUrl = "test";
    private const string QueueName = "SQS";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Poll for up to 10 messages using long polling (10 seconds)
            var receiveResponse = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = QueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 1
            }, stoppingToken);

            if (!receiveResponse.Messages.Any()) continue;

            // Process all messages concurrently.
            receiveResponse.Messages.ForEach(message =>
                Task.Run(() => ProcessMessageAsync(message, stoppingToken), stoppingToken));
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken stoppingToken)
    {
        try
        {
            // Deserialize the message body to ProcessingMeasurements
            var processingMeasurements = JsonSerializer.Deserialize<ProcessingMeasurements>(message.Body);

            if (processingMeasurements != null)
            {
                ProcessMessage(processingMeasurements);
            }
            else
            {
                logger.LogWarning("Unable to deserialize message: {MessageBody}", message.Body);
            }

            // Delete the message after processing.
            await sqs.DeleteMessageAsync(new DeleteMessageRequest
            {
                QueueUrl = QueueUrl,
                ReceiptHandle = message.ReceiptHandle
            }, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {Message}", message.Body);
        }
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
}
