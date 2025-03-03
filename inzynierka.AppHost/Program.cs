using Confluent.Kafka;
using Confluent.Kafka.Admin;
using RabbitMQ.Client;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("messagingKafka")
    .WithKafkaUI(kafkaUi =>
        kafkaUi
            .WithHostPort(9100)
            .WithContainerRuntimeArgs("--memory=1g", "--cpus=1"))
    .WithContainerRuntimeArgs("--memory=1g", "--cpus=1");

var rabbit = builder.AddRabbitMQ("messagingRabbitMQ")
    .WithContainerRuntimeArgs("--memory=1g", "--cpus=1");

var mongo = builder.AddMongoDB("mongo")
    .WithDataBindMount(@"C:\MongoDB\Data")
    .WithMongoExpress();

var mongodb = mongo.AddDatabase("mongodb");

builder.Eventing.Subscribe<ResourceReadyEvent>(kafka.Resource, async (@event, ct) =>
{
    var cs = await kafka.Resource.ConnectionStringExpression.GetValueAsync(ct);
    var config = new AdminClientConfig
    {
        BootstrapServers = cs
    };

    using var adminClient = new AdminClientBuilder(config).Build();
    try
    {
        await adminClient.CreateTopicsAsync(
        [
            new TopicSpecification { Name = "test", NumPartitions = 1, ReplicationFactor = 1 },
        ]);
        Console.WriteLine("Topic created");
    }
    catch (CreateTopicsException e)
    {
        Console.WriteLine($"An error occurred creating topic: {e.Message}");
        throw;
    }
});

builder.Eventing.Subscribe<ResourceReadyEvent>(rabbit.Resource, async (@event, ct) =>
{
    var cs = await rabbit.Resource.ConnectionStringExpression.GetValueAsync(ct);
    if (cs is not null)
    {
        var factory = new ConnectionFactory { Uri = new Uri(cs) };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(
            exchange: "testEx",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: true,
            arguments: null);

        channel.QueueDeclare(
            queue: "testQ",
            durable: true,
            exclusive: false,
            autoDelete: true,
            arguments: null);
    }

    Console.WriteLine("Exchange 'testEx' has been declared.");
});


builder.AddProject<Projects.QueueApi>("queueapi")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.AddProject<Projects.KafkaWorker>("kafkaworker")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.AddProject<Projects.RabbitWorker>("rabbitworker")
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(mongodb)
    .WaitFor(mongodb);

builder.AddProject<Projects.SqsWorker>("sqsworker")
    .WithReference(mongodb)
    .WaitFor(mongodb); ;

builder.Build().Run();
