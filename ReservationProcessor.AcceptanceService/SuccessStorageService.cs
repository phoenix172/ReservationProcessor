using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ReservationProcessor.ServiceDefaults.Messaging;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

namespace ReservationProcessor.AcceptanceService;

public class SuccessStorageService : IMessageHandler<ValidationSuccessMessage>
{
    private readonly SqlConnection _connection;
    private readonly ILogger<SuccessStorageService> _logger;
    private readonly IMessagePublisher<ValidationSuccessSavedMessage> _publisher;

    public SuccessStorageService(SqlConnection connection, ILogger<SuccessStorageService> logger, IMessagePublisher<ValidationSuccessSavedMessage> publisher)
    {
        _connection = connection;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task Handle(ValidationSuccessMessage message)
    {
        try
        {
            Guid validationSuccessId = await SaveValidationSuccess(message);
            _logger.LogInformation($"Successfully saved ValidationSuccess message ReservationId: {message.ReservationId}");

            await _publisher.Publish(new ValidationSuccessSavedMessage()
            {
                ReservationId = message.ReservationId,
                ValidationDate = message.ValidationDate,
                ValidationSuccessId = validationSuccessId
            });
        }
        catch(Exception ex) 
        {
            _logger.LogError(ex, $"Error saving ValidationSuccess message ReservationId:{message.ReservationId}");
        }
    }

    private async Task<Guid> SaveValidationSuccess(ValidationSuccessMessage request)
    {
        var parameters = new DynamicParameters(request);
        parameters.Add("@AcceptanceId", dbType: DbType.Guid, direction: ParameterDirection.Output);
        await _connection.ExecuteAsync("StoreAcceptedRequest", parameters, commandType: CommandType.StoredProcedure);
        var reservationId = parameters.Get<Guid>("@AcceptanceId");
        return reservationId;
    }
}