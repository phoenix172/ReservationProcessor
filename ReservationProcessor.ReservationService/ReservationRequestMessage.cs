namespace ReservationProcessor.ReservationService;

public class ReservationRequestMessage
{
    public string ClientName { get; set; }
    public string ClientTelephone { get; set; }
    public int NumberOfReservedTable { get; set; }
    public string DateOfReservation { get; set; }
}