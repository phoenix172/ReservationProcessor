using ReservationProcessor.ServiceDefaults.Messaging;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

namespace ReservationProcessor.AcceptanceService;

public class SuccessStorageService : IMessageHandler<ValidationSuccessMessage>
{
    public async Task Handle(ValidationSuccessMessage message)
    {
        Console.WriteLine(message.ReservationId);
    }
}