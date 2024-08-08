namespace ReservationProcessor.ServiceDefaults.Messaging.Data;

public class ValidationSuccessSavedMessage
{
    public Guid ReservationId { get; set; }
    public Guid ValidationSuccessId { get; set; }
    public DateTime ValidationDate { get; set; }
}