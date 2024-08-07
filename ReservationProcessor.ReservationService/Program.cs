
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
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.AddRabbitMQ("MessageBus")
            .AddMessageConsumer<ReservationRequestMessage>("Reservations_RabbitMQ")
            .AddMessagePublisher<ValidationSuccessMessage>("Success_RabbitMQ")
            .AddMessagePublisher<ValidationFailureMessage>("Fail_RabbitMQ")
            .AddMessageHandler<ReservationRequestMessage, ValidationService>();

        builder.AddKeyedSqlServerClient("MasterDB");
        builder.AddSqlServerClient("ReservationsDB");

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        await app.InitializeDatabase();

        app.Run();
    }
}