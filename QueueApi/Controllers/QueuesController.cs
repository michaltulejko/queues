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
    [HttpGet("kafka", Name = "GetKafkaMessagesStats")]
    public async Task<ActionResult> GetKafka(int messagesCount)
    {
        logger.LogInformation("GetKafkaMessagesStats called");
        var ids = new ConcurrentBag<Guid>();
        Parallel.For(0, messagesCount, i =>
        {
            ids.Add(Guid.NewGuid());
        });

        await Parallel.ForEachAsync(ids, async (id, cancellationToken) =>
        {
            var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await kafkaProducer.ProduceAsync("test", id, unixTimeSeconds, cancellationToken);
        });

        return Ok();
    }

    [HttpGet("rabbit", Name = "GetRabbitMessagesStats")]
    public async Task<ActionResult> GetRabbit(int messagesCount)
    {
        logger.LogInformation("GetRabbitMessagesStats called");
        var ids = new ConcurrentBag<Guid>();

        Parallel.For(0, messagesCount, i =>
        {
            ids.Add(Guid.NewGuid());
        });

        await Parallel.ForEachAsync(ids, async (id, cancellationToken) =>
        {
            var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await rabbitProducer.ProduceAsync("test", id, unixTimeSeconds, cancellationToken);
        });

        return Ok();
    }

    [HttpGet("sqs", Name = "GetSqsMessagesStats")]
    public async Task<ActionResult> GetSqs(int messagesCount)
    {
        logger.LogInformation("GetSqsMessagesStats called");
        var ids = new ConcurrentBag<Guid>();

        Parallel.For(0, messagesCount, i =>
        {
            ids.Add(Guid.NewGuid());
        });

        await Parallel.ForEachAsync(ids, async (id, cancellationToken) =>
        {
            var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            await sqsProducer.ProduceAsync("test", id, unixTimeSeconds, cancellationToken);
        });

        return Ok();
    }
}
