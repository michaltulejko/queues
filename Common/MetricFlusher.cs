using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Common;

public class MetricsFlusher(ILogger<MetricsFlusher> logger, MetricsCollector collector) : BackgroundService
{
    // File to which we append the metrics.
    private readonly string _filePath = "metrics.jsonl";
    private readonly MetricsCollector _collector = collector;
    private readonly ILogger<MetricsFlusher> _logger = logger;

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
        var measurements = _collector.DequeueAll().ToList();
        if (!measurements.Any())
        {
            return;
        }

        // Build the JSON Lines string.
        var sb = new StringBuilder();
        foreach (var measurement in measurements)
        {
            // Serialize each measurement as a JSON object
            var json = JsonSerializer.Serialize(measurement);
            sb.AppendLine(json);
        }

        // Append asynchronously to the file (non-blocking)
        await File.AppendAllTextAsync(_filePath, sb.ToString());
        _logger.LogInformation("Flushed {Count} metrics to file.", measurements.Count);
    }
}