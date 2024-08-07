namespace ReservationProcessor.ServiceDefaults.Messaging.Data;

public class ValidationSuccessMessage
{
    public DateTime ValidationDate { get; set; }
    public Guid ReservationId { get; set; }
}