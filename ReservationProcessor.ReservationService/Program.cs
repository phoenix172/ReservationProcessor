
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using ReservationProcessor.ServiceDefaults;
using ReservationProcessor.ServiceDefaults.Messaging;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

namespace ReservationProcessor.ReservationService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();

        builder.AddRabbitMQ("MessageBus")
            .AddMessageConsumer<ReservationRequestMessage>("Reservations_RabbitMQ")
            .AddMessagePublisher<ValidationSuccessMessage>("Success_RabbitMQ")
            .AddMessagePublisher<ValidationFailureMessage>("Fail_RabbitMQ")
            .AddMessageHandler<ReservationRequestMessage, ValidationService>()
            .AddMessageConsumer<ValidationSuccessSavedMessage>("Response_RabbitMQ")
            .AddMessageHandler<ValidationSuccessSavedMessage, ResponseStorageService>();

        builder.AddKeyedSqlServerClient("MasterDB");
        builder.AddSqlServerClient("ReservationsDB");

        var app = builder.Build();

        await app.InitializeDatabase();

        await app.RunAsync();
    }
}