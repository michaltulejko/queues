using Microsoft.AspNetCore.Mvc;
using QueueApi.Kafka.Interfaces;
using QueueApi.Rabbit.Interfaces;
using QueueApi.Sqs.Interfaces;
using System.Collections.Concurrent;

namespace QueueApi.Controllers;

[ApiController]
[Route("[controller]")]
public class QueuesController(
    ILogger<QueuesController> logger,
    IKafkaProducer kafkaProducer,
    IRabbitProducer rabbitProducer,
    ISqsProducer sqsProducer)
    : ControllerBase
{

    [HttpGet(Name = "GetMessagesStats")]
    public async Task<IEnumerable<string>> Get()
    {
        logger.LogInformation("GetMessagesStats called");

        var ids = new ConcurrentBag<Guid>();

        Parallel.For(0, 10000, i =>
        {
            ids.Add(Guid.NewGuid());
        });

        await Parallel.ForEachAsync(ids, async (id, cancellationToken) =>
        {
            var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await kafkaProducer.ProduceAsync("test", id, unixTimeSeconds, cancellationToken);
            unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await rabbitProducer.ProduceAsync("test", id, unixTimeSeconds, cancellationToken);
            unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await sqsProducer.ProduceAsync("test", id, unixTimeSeconds, cancellationToken);
        });

        return new[] { "value1", "value2" };
    }
}
