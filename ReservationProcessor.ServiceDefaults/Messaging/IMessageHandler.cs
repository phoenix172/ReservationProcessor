namespace ReservationProcessor.ServiceDefaults.Messaging;

public interface IMessageHandler<T>
{
    Task Handle(T message);
}