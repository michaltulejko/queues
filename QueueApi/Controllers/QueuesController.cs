using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using QueueApi.Kafka.Interfaces;
using QueueApi.Models;
using QueueApi.Rabbit.Interfaces;
using QueueApi.Sqs.Interfaces;
using System.Collections.Concurrent;
using System.Text;

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

    [HttpGet("export-csv", Name = "ExportQueueStatisticsCSV")]
    public async Task<ActionResult> ExportQueueStatisticsCSV(string queueName, int recordsAmount)
    {
        logger.LogInformation("ExportQueueStatisticsCSV called for queue: {QueueName}, records: {RecordsAmount}",
            queueName, recordsAmount);

        try
        {
            var database = mongoClient.GetDatabase("metrics");
            var collection = database.GetCollection<BsonDocument>("delay");

            var filter = Builders<BsonDocument>.Filter.Eq("queue", queueName);
            var options = new FindOptions<BsonDocument>
            {
                Limit = recordsAmount,
                Sort = Builders<BsonDocument>.Sort.Descending("processingTime")
            };

            var documents = await collection.FindAsync(filter, options);
            var stats = await documents.ToListAsync();

            var entities = stats.Select(doc => BsonSerializer.Deserialize<DelayEntity>(doc)).ToList();

            if (!entities.Any())
            {
                return NotFound($"No metrics found for queue '{queueName}'");
            }

            // Create CSV content
            var csvBuilder = new StringBuilder();

            // Add CSV header using the exact property names from the DelayEntity class
            csvBuilder.AppendLine("Id,QueueCreationTime,QueueCreationDelay,ProcessingTime,ProcessingDelay,QueueName");

            // Add data rows with raw values
            foreach (var entity in entities)
            {
                csvBuilder.AppendLine(string.Join(",",
                    entity.Id,
                    entity.QueueCreationTime,
                    entity.QueueCreationDelay,
                    entity.ProcessingTime,
                    entity.ProcessingDelay,
                    entity.QueueName));
            }

            // Get the bytes of the CSV content
            var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

            // Set current timestamp for filename
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"queue_metrics_{queueName}_{timestamp}.csv";

            // Return as file download
            return File(csvBytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting CSV data for queue: {QueueName}", queueName);
            return StatusCode(500, "An error occurred while generating the CSV file");
        }
    }


}
