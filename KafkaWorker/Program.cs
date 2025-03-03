using Common;
using Confluent.Kafka;
using inzynierka.ServiceDefaults;

namespace KafkaWorker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();

        builder.AddMongoDBClient(connectionName: "mongodb");

        // Services
        builder.Services.AddSingleton<MetricsCollector>();

        builder.Services.AddHostedService<MetricsFlusher>();
        builder.Services.AddHostedService<KafkaWorker>();


        builder.AddKafkaConsumer<string, long>("messagingKafka", static settings =>
        {
            settings.Config.GroupId = "test-group";
            settings.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
            settings.Config.EnableAutoCommit = false;
        });

        var host = builder.Build();
        host.Run();
    }
}