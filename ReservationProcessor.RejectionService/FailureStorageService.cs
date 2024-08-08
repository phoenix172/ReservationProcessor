using System.Data;
using Dapper;
using Npgsql;
using ReservationProcessor.ServiceDefaults.Messaging.Data;
using ReservationProcessor.ServiceDefaults.Messaging;

namespace ReservationProcessor.RejectionService;

public class FailureStorageService: IMessageHandler<ValidationFailureMessage>
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<FailureStorageService> _logger;

    public FailureStorageService(NpgsqlConnection connection, ILogger<FailureStorageService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task Handle(ValidationFailureMessage message)
    {
        try
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("reservationid", message.ReservationId, dbType: DbType.Guid);
            parameters.Add("validationdate", message.ValidationDate,dbType: DbType.DateTime2);
            parameters.Add("errors", string.Join(',',message.Errors) ,DbType.String);
            parameters.Add("rejectionid",Guid.Empty,dbType: DbType.Guid,direction: ParameterDirection.Output);
            
            await _connection.ExecuteAsync("public.\"StoreReservationRejection\"", parameters,
                commandType: CommandType.StoredProcedure);
            _logger.LogInformation($"ValidationFailure saved for ReservationId:{message.ReservationId} has been saved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to save ValidationFailure for ReservationId:{message.ReservationId}");
        }
    }
}