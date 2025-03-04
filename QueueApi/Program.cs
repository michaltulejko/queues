using Amazon.Runtime;
using Amazon.SQS;
using inzynierka.ServiceDefaults;
using QueueApi.Kafka;
using QueueApi.Kafka.Interfaces;
using QueueApi.Rabbit;
using QueueApi.Rabbit.Interfaces;
using QueueApi.Sqs;
using QueueApi.Sqs.Interfaces;

namespace QueueApi;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddSwaggerGen();

        // AWS configuration
        var awsOptions = builder.Configuration.GetAWSOptions();

        // Extract dummy credentials from configuration
        var accessKey = builder.Configuration["AWS:AccessKey"];
        var secretKey = builder.Configuration["AWS:SecretKey"];

        // Override the credentials so that the SDK doesn't fall back to IMDS
        awsOptions.Credentials = new BasicAWSCredentials(accessKey, secretKey);

        // Add AWS options and services
        builder.Services.AddDefaultAWSOptions(awsOptions);
        builder.Services.AddAWSService<IAmazonSQS>();

        // Kafka configuration
        builder.AddKafkaProducer<string, long>("messagingKafka");

        //RabbitMQ configuration
        builder.AddRabbitMQClient("messagingRabbitMQ");

        //SQS configuration
        builder.AddMongoDBClient(connectionName: "mongodb");

        // Services
        builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();
        builder.Services.AddScoped<IRabbitProducer, RabbitProducer>();
        builder.Services.AddScoped<ISqsProducer, SqsProducer>();

        // Add CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        // Use CORS
        app.UseCors("AllowFrontend");

        app.Run();

    }
}
