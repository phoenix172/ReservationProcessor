using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

namespace ReservationProcessor.ServiceDefaults.Messaging;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddRabbitMQ(this IHostApplicationBuilder hostBuilder, string connectionName)
    {
        hostBuilder.AddRabbitMQClient(connectionName, configureConnectionFactory: x=>x.DispatchConsumersAsync=true);
        hostBuilder.Services.AddSingleton(svc =>
        {
            var factory = svc.GetRequiredService<RabbitMQ.Client.IConnectionFactory>();
            return factory.CreateConnection();
        });
        return hostBuilder.Services;
    }

    public static IServiceCollection AddMessageConsumer<T>(this IServiceCollection services, string queueName)
    {
        return services.AddHostedService<IMessageConsumer<T>>(svc =>
        {
            var consumer = new MessageConsumer<T>(
                queueName,
                svc.GetRequiredService<IConnection>(),
                svc.GetRequiredService<IHostApplicationLifetime>(),
                svc.GetRequiredService<ILogger<MessageConsumer<T>>>(),
                svc.GetRequiredService<IServiceScopeFactory>());
            return consumer;
        });
    }

    public static IServiceCollection AddMessagePublisher<T>(this IServiceCollection services, string queueName)
    {
        return services.AddSingleton<IMessagePublisher<T>>(svc => 
            new MessagePublisher<T>(
            queueName,
            svc.GetRequiredService<IConnection>()));
    }

    public static IServiceCollection AddMessageHandler<T, THandler>(this IServiceCollection services) where THandler : class, IMessageHandler<T>
    {
        return services.AddScoped<IMessageHandler<T>, THandler>();
    }
}