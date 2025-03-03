using Amazon.SQS;
using Amazon.SQS.Model;
using Common;

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
            // Assume the message body is a Unix timestamp (in seconds).
            if (long.TryParse(message.Body, out var eventTime))
            {
                var now = DateTime.UtcNow;
                var eventDateTime = DateTimeOffset.FromUnixTimeSeconds(eventTime).DateTime;
                var delay = now - eventDateTime;
                logger.LogInformation("Delay between event creation and processing: {Delay}", delay);

                // Record the metric in a thread-safe, non-blocking way.
                metricsCollector.Enqueue(new DelayMeasurement(eventTime, delay, QueueName));
            }
            else
            {
                logger.LogWarning("Unable to parse event time from message: {MessageBody}", message.Body);
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
}