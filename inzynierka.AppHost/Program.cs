using Confluent.Kafka;
using Confluent.Kafka.Admin;
using RabbitMQ.Client;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("messagingKafka")
    .WithKafkaUI(kafkaUi =>
        kafkaUi
            .WithHostPort(9100)
            .WithContainerRuntimeArgs("--memory=1g", "--cpus=0.5"))
    .WithContainerRuntimeArgs("--memory=1g", "--cpus=0.5");

var rabbit = builder.AddRabbitMQ("messagingRabbitMQ")
    .WithContainerRuntimeArgs("--memory=1g", "--cpus=0.5");

var mongo = builder.AddMongoDB("mongo")
    .WithDataBindMount(@"C:\MongoDB\Data")
    .WithContainerRuntimeArgs("--memory=1g", "--cpus=0.5")
    .WithMongoExpress(express => 
        express.WithContainerRuntimeArgs("--memory=1g", "--cpus=0.5"));

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

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: "testEx",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null);

        await channel.QueueDeclareAsync(
            queue: "testQ",
            durable: true,
            exclusive: false,
            autoDelete: false,
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
