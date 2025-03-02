using inzynierka.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
//RabbitMQ configuration
builder.AddRabbitMQClient("messagingRabbitMQ");

builder.Services.AddHostedService<RabbitWorker.RabbitWorker>();

var host = builder.Build();
host.Run();
