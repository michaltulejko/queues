using System.Collections.Concurrent;

namespace Common;

public class MetricsCollector
{
    // A thread-safe, lock-free queue to store delay measurements.
    private readonly ConcurrentQueue<DelayMeasurement> _queue = new();

    public void Enqueue(DelayMeasurement measurement)
    {
        _queue.Enqueue(measurement);
    }

    // Try to dequeue all available measurements.
    public IEnumerable<DelayMeasurement> DequeueAll()
    {
        while (_queue.TryDequeue(out var measurement))
        {
            yield return measurement;
        }
    }
}

public record DelayMeasurement(
    long QueueCreationTime,
    TimeSpan QueueCreationDelay,
    long ProcessingTime,
    TimeSpan ProcessingDelay,
    string QueueName)
{
}
