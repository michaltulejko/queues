using Amazon.SQS;
using Common;
using inzynierka.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// AWS configuration
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddSingleton<MetricsCollector>();

builder.Services.AddHostedService<SqsWorker.SqsWorker>();

var host = builder.Build();
host.Run();
