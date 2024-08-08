
using ReservationProcessor.ServiceDefaults.Messaging;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

namespace ReservationProcessor.AcceptanceService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();

        builder.AddKeyedSqlServerClient("MasterDB", x=>Console.WriteLine(x.ConnectionString));
        builder.AddSqlServerClient("AcceptanceDB", x=>Console.WriteLine(x.ConnectionString));

        builder.AddRabbitMQ("MessageBus")
            .AddMessageConsumer<ValidationSuccessMessage>("Success_RabbitMQ")
            .AddMessageHandler<ValidationSuccessMessage, SuccessStorageService>()
            .AddMessagePublisher<ValidationSuccessSavedMessage>("Response_RabbitMQ");

        var app = builder.Build();

        await app.InitializeDatabase();

        await app.RunAsync();
    }
}