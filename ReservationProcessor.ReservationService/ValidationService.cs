using Microsoft.Data.SqlClient;
using ReservationProcessor.ReservationService.Controllers;
using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using ReservationProcessor.ServiceDefaults.Messaging;

namespace ReservationProcessor.ReservationService;

public class DelayService : IMessageHandler<ReservationRequestMessage>
{
    private readonly ILogger<DelayService> _logger;
    
    private readonly IMessagePublisher<ReservationRequestMessage> _publisher;

    public DelayService(ILogger<DelayService> logger, IMessagePublisher<ReservationRequestMessage> publisher)
    {
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(ReservationRequestMessage message)
    {
        Enumerable.Range(0,20).ToList().ForEach(x=>
        {
            _publisher.Publish(message);
            Task.Delay(500);
            _logger.LogInformation($"DelayService {x}");
        });
        //await _publisher.Publish(message);
        _logger.LogInformation("Message published");
    }
}

public class ValidationService : IMessageHandler<ReservationRequestMessage>
{
    private readonly SqlConnection _sqlConnection;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(SqlConnection sqlConnection, ILogger<ValidationService> logger)
    {
        _sqlConnection = sqlConnection;
        _logger = logger;
    }

    public async Task Handle(ReservationRequestMessage message)
    {
        var request = new ReservationRequest()
        {
            RawRequest = JsonSerializer.Serialize(message),
            DateCreated = DateTime.Now,
            ValidationResult = Validate(message)
        };
        await _sqlConnection.ExecuteAsync("StoreReservationRequest", request, commandType: CommandType.StoredProcedure);
    }

    private ValidationResult Validate(ReservationRequestMessage message)
    {
        bool valid = true;
        valid &= !string.IsNullOrWhiteSpace(message.ClientName);
        valid &= message.ClientTelephone.All(char.IsDigit) && message.ClientTelephone.Length == 10;
        valid &= message.NumberOfReservedTable >= 0;
        valid &= DateTime.TryParseExact(message.DateOfReservation, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        return valid ? ValidationResult.Ok : ValidationResult.Fail;
    }
}