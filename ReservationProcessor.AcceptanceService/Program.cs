
using ReservationProcessor.ServiceDefaults.Messaging;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

namespace ReservationProcessor.AcceptanceService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.AddKeyedSqlServerClient("MasterDB");
        builder.AddSqlServerClient("AcceptanceDB");

        // Add services to the container.
        builder.AddRabbitMQ("MessageBus")
            .AddMessageConsumer<ValidationSuccessMessage>("Success_RabbitMQ")
            .AddMessageHandler<ValidationSuccessMessage, SuccessStorageService>()
            .AddMessagePublisher<ValidationSuccessSavedMessage>("Response_RabbitMQ");

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