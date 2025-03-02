using Amazon.SQS;
using inzynierka.ServiceDefaults;
using LocalStack.Client.Extensions;
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
        builder.Services.AddLocalStack(builder.Configuration);
        builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
        var test = builder.Configuration.GetAWSOptions();
        builder.Services.AddAWSService<IAmazonSQS>();

        // Kafka configuration
        builder.AddKafkaProducer<string, long>("messagingKafka");

        //RabbitMQ configuration
        builder.AddRabbitMQClient("messagingRabbitMQ");

        // Services
        builder.Services.AddScoped<IKafkaProducer, KafkaProducer>();
        builder.Services.AddScoped<IRabbitProducer, RabbitProducer>();
        builder.Services.AddScoped<ISqsProducer, SqsProducer>();

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

        app.Run();

    }
}
