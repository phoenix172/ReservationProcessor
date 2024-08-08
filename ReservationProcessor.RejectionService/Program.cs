
using ReservationProcessor.ReservationService;
using ReservationProcessor.ServiceDefaults.Messaging.Data;
using ReservationProcessor.ServiceDefaults.Messaging;

namespace ReservationProcessor.RejectionService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.AddKeyedNpgsqlDataSource("PostgresDefaultDB");
        builder.AddNpgsqlDataSource("RejectionsDB");

        builder.AddRabbitMQ("MessageBus")
            .AddMessageConsumer<ValidationFailureMessage>("Fail_RabbitMQ")
            .AddMessageHandler<ValidationFailureMessage, FailureStorageService>();

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

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

        await app.RunAsync();
    }
}
