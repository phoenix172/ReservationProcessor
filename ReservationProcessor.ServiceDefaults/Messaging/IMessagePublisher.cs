namespace ReservationProcessor.ServiceDefaults.Messaging;

public interface IMessagePublisher<T> : IDisposable
{
    string QueueName { get; }
    Task<bool> Publish(T message);
}