using ReservationProcessor.ServiceDefaults.Messaging.Data;
using ReservationProcessor.ServiceDefaults.Messaging;

namespace ReservationProcessor.RejectionService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();

        builder.AddKeyedNpgsqlDataSource("PostgresDefaultDB", x=>Console.WriteLine(x.ConnectionString));
        builder.AddNpgsqlDataSource("RejectionsDB", x=>Console.WriteLine(x.ConnectionString));

        builder.AddRabbitMQ("MessageBus")
            .AddMessageConsumer<ValidationFailureMessage>("Fail_RabbitMQ")
            .AddMessageHandler<ValidationFailureMessage, FailureStorageService>();

        var app = builder.Build();

        await app.InitializeDatabase();

        await app.RunAsync();
    }
}
