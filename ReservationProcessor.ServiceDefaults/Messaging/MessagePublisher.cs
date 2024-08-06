using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ReservationProcessor.ServiceDefaults.Messaging
{
    public class MessagePublisher<T> : IMessagePublisher<T>
    {
        public string QueueName { get; }

        private readonly ConcurrentBag<IModel> _channels;
        private readonly ThreadLocal<IModel> _channel;

        public MessagePublisher(string queueName, IConnection connection)
        {
            QueueName = queueName;
            _channels = new ConcurrentBag<IModel>();
            _channel = new ThreadLocal<IModel>(() =>
            {
                var channel = connection.CreateModel();
                channel.QueueDeclare(queue: QueueName, durable: false, exclusive: false, autoDelete: false);
                channel.ConfirmSelect();
                _channels.Add(channel);
                return channel;
            });
        }

        public async Task<bool> Publish(T message)
        {
            var result = await Task.Run<bool>(() =>
            {
                var messageJson = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(messageJson);
                var channel = _channel.Value;
                channel.BasicPublish(exchange: string.Empty, routingKey: QueueName, basicProperties: null, body: body);
                return channel.WaitForConfirms();
            });
            return result;
        }

        public void Dispose()
        {
            foreach (var channel in _channels)
            {
                channel.Dispose();
            }
        }
    }
}
