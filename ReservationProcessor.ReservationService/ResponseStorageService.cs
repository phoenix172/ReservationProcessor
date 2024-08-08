using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using ReservationProcessor.ServiceDefaults.Messaging;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

namespace ReservationProcessor.ReservationService;

public class ResponseStorageService : IMessageHandler<ValidationSuccessSavedMessage>
{
    private readonly SqlConnection _connection;
    private readonly ILogger<ResponseStorageService> _logger;

    public ResponseStorageService(SqlConnection connection, ILogger<ResponseStorageService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task Handle(ValidationSuccessSavedMessage message)
    {
        try
        {
            Guid responseId = await SaveValidationSuccessResponse(message);
            _logger.LogInformation($"Successfully Response (ValidationSuccessSaved) message ReservationId: {message.ReservationId}, ResponseId: {responseId}");
        }
        catch(Exception ex) 
        {
            _logger.LogError(ex, $"Error saving Response (ValidationSuccessSaved) message ReservationId: {message.ReservationId}");
        }
    }

    private async Task<Guid> SaveValidationSuccessResponse(ValidationSuccessSavedMessage request)
    {
        var parameters = new DynamicParameters(request);
        parameters.Add("@ResponseId", dbType: DbType.Guid, direction: ParameterDirection.Output);
        await _connection.ExecuteAsync("StoreSuccessResponse", parameters, commandType: CommandType.StoredProcedure);
        var responseId = parameters.Get<Guid>("@ResponseId");
        return responseId;
    }
}