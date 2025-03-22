using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Common;

public class MetricsFlusher(
    ILogger<MetricsFlusher> logger,
    MetricsCollector collector,
    IMongoClient mongoClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Flush metrics every 5 seconds (adjust as needed)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await FlushMetricsAsync();
        }
    }

    private async Task FlushMetricsAsync()
    {
        var measurements = collector.DequeueAll().ToList();
        if (!measurements.Any())
        {
            return;
        }

        var database = mongoClient.GetDatabase("metrics");
        var collection = database.GetCollection<BsonDocument>("delay");

        foreach (var document in measurements.Select(measurement => new BsonDocument
                 {
                     { "timestamp", measurement.ProcessingTime },
                     { "delay", measurement.ProcessingDelay.TotalMilliseconds },
                     { "queue", measurement.QueueName }
                 }))
        {
            await collection.InsertOneAsync(document);
        }

        logger.LogInformation("Flushed {Count} metrics to file.", measurements.Count);
    }
}