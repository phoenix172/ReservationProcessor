using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ReservationProcessor.ServiceDefaults.Messaging;

public interface IMessageConsumer<T> : IHostedService
{
    string QueueName { get; }
}

public class MessageConsumer<T> : BackgroundService, IMessageConsumer<T>
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<MessageConsumer<T>> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public MessageConsumer(string queueName,
        IConnection connection, IHostApplicationLifetime lifetime,
        ILogger<MessageConsumer<T>> logger, IServiceScopeFactory serviceScopeFactory)
    {
        QueueName = queueName;
        _connection = connection;
        _lifetime = lifetime;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _channel = _connection.CreateModel();
    }

    public string QueueName { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare(queue: QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += ConsumerOnReceived;
        _channel.BasicConsume(QueueName, false, consumer);

        _lifetime.ApplicationStopping.Register(() =>
        {
            _channel.Dispose();
            _connection.Dispose();
        });
    }

    private async Task ConsumerOnReceived(object sender, BasicDeliverEventArgs @event)
    {
        IEnumerable<Task> tasks = Enumerable.Empty<Task>();
        AsyncServiceScope? scope = null;
        try
        {
            var message = JsonSerializer.Deserialize<T>(@event.Body.Span);
            if (message == null)
                throw new ArgumentException(
                    $"Failed to parse JSON or message was empty {typeof(T)} deliveryTag: {@event.DeliveryTag}");

            scope = _serviceScopeFactory.CreateAsyncScope();
            var handlers = scope.Value.ServiceProvider.GetRequiredService<IEnumerable<IMessageHandler<T>>>();
            tasks = handlers.Select(x => Task.Run((Func<Task?>)(() => x.Handle(message))));
            _channel.BasicAck(@event.DeliveryTag, false);
            _logger.LogInformation(
                $"Successfully received and started processing message {typeof(T)} deliveryTag: {@event.DeliveryTag}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to parse message {typeof(T)} deliveryTag: {@event.DeliveryTag}");
            _channel.BasicReject(@event.DeliveryTag, false);
        }
        finally
        {
            await Task.WhenAll(tasks);
            if(scope!=null)
                await (scope.Value.DisposeAsync());
            _logger.LogInformation(
                $"All tasks have finished for message {typeof(T)} deliveryTag: {@event.DeliveryTag}");
        }
    }
}