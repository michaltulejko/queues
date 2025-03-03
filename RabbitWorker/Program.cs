using Common;
using inzynierka.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
//RabbitMQ configuration
builder.AddRabbitMQClient("messagingRabbitMQ");
builder.AddMongoDBClient(connectionName: "mongodb");

// Services
builder.Services.AddSingleton<MetricsCollector>();

builder.Services.AddHostedService<MetricsFlusher>();
builder.Services.AddHostedService<RabbitWorker.RabbitWorker>();


var host = builder.Build();
host.Run();
