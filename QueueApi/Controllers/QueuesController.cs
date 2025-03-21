using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using QueueApi.Kafka.Interfaces;
using QueueApi.Models;
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
    ISqsProducer sqsProducer,
    IMongoClient mongoClient)
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

        var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        await Parallel.ForEachAsync(ids, async (id, cancellationToken) =>
        {
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

        var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        await Parallel.ForEachAsync(ids, async (id, cancellationToken) =>
        {
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

        var unixTimeSeconds = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        await Parallel.ForEachAsync(ids, async (id, cancellationToken) =>
        {
            await sqsProducer.ProduceAsync("test", id, unixTimeSeconds, cancellationToken);
        });

        return Ok();
    }

    [HttpGet("statistics", Name = "GetQueueStatistics")]
    public async Task<ActionResult> GetQueueStatistics(string queueName, int recordsAmount)
    {
        var database = mongoClient.GetDatabase("metrics");
        var collection = database.GetCollection<BsonDocument>("delay");

        var filter = Builders<BsonDocument>.Filter.Eq("queue", queueName);
        var options = new FindOptions<BsonDocument>
        {
            Limit = recordsAmount
        };

        var documents = await collection.FindAsync(filter, options);
        var stats = await documents.ToListAsync();

        var entities = stats.Select(doc => BsonSerializer.Deserialize<DelayEntity>(doc)).ToList();
        return Ok(entities);
    }
}
