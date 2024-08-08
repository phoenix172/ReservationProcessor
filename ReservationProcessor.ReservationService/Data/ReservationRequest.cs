namespace ReservationProcessor.ReservationService.Data;

public class ReservationRequest
{
    public string RawRequest { get; set; }
    public DateTime DateCreated { get; set; }
    public ValidationResult? ValidationResult { get; set; }
}