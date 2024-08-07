using ReservationProcessor.ServiceDefaults.Messaging.Data;
using ReservationProcessor.ServiceDefaults.Messaging;

namespace ReservationProcessor.RejectionService;

public class FailureStorageService: IMessageHandler<ValidationFailureMessage>
{
    public async Task Handle(ValidationFailureMessage message)
    {
        Console.WriteLine(message.ReservationId);
    }
}