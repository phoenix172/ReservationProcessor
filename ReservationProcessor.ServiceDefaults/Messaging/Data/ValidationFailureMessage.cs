namespace ReservationProcessor.ServiceDefaults.Messaging.Data;

public class ValidationFailureMessage
{
    public DateTime ValidationDate { get; set; }
    public Guid ReservationId { get; set; }
    public IReadOnlyCollection<string> Errors { get; set; }
}