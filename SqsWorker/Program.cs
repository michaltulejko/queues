using Amazon.Runtime;
using Amazon.SQS;
using Common;
using inzynierka.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// AWS configuration
// AWS configuration
var awsOptions = builder.Configuration.GetAWSOptions();

// Extract dummy credentials from configuration
var accessKey = builder.Configuration["AWS:AccessKey"];
var secretKey = builder.Configuration["AWS:SecretKey"];

// Override the credentials so that the SDK doesn't fall back to IMDS
awsOptions.Credentials = new BasicAWSCredentials(accessKey, secretKey);

// Add AWS options and services
builder.Services.AddDefaultAWSOptions(awsOptions); builder.Services.AddAWSService<IAmazonSQS>();

builder.AddMongoDBClient(connectionName: "mongodb");

//Services
builder.Services.AddSingleton<MetricsCollector>();

builder.Services.AddHostedService<SqsWorker.SqsWorker>();
builder.Services.AddHostedService<MetricsFlusher>();

var host = builder.Build();
host.Run();
