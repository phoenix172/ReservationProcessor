using Microsoft.Data.SqlClient;
using ReservationProcessor.ReservationService.Controllers;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;
using Dapper;
using ReservationProcessor.ServiceDefaults.Messaging;
using ReservationProcessor.ServiceDefaults.Messaging.Data;

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
    private readonly IMessagePublisher<ValidationSuccessMessage> _successPublisher;
    private readonly IMessagePublisher<ValidationFailureMessage> _failPublisher;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(SqlConnection sqlConnection,
        IMessagePublisher<ValidationSuccessMessage> successPublisher,
        IMessagePublisher<ValidationFailureMessage> failPublisher,
        ILogger<ValidationService> logger)
    {
        _sqlConnection = sqlConnection;
        _successPublisher = successPublisher;
        _failPublisher = failPublisher;
        _logger = logger;
    }

    public async Task Handle(ReservationRequestMessage message)
    {
        var request = new ReservationRequest()
        {
            RawRequest = JsonSerializer.Serialize(message),
            DateCreated = DateTime.Now,
            ValidationResult = Validate(message, out var errors)
        };

        var reservationId = await SaveReservationRequest(request); //Task 1

        if (request.ValidationResult == ValidationResult.Ok) //Task 2
        {
            await PublishSuccess(reservationId);
        }
        else
        {
            await PublishFail(reservationId, errors);
        }
    }

    private async Task PublishFail(Guid reservationId, IReadOnlyCollection<string> errors)
    {
        var failMessage = new ValidationFailureMessage()
        {
            ReservationId = reservationId,
            ValidationDate = DateTime.Now,
            Errors = errors
        };
        await _failPublisher.Publish(failMessage);
    }

    private async Task PublishSuccess(Guid reservationId)
    {
        var successMessage = new ValidationSuccessMessage()
        {
            ReservationId = reservationId,
            ValidationDate = DateTime.Now
        };
        await _successPublisher.Publish(successMessage);
    }

    private async Task<Guid> SaveReservationRequest(ReservationRequest request)
    {
        var parameters = new DynamicParameters(request);
        parameters.Add("@ReservationId", dbType: DbType.Guid, direction: ParameterDirection.Output);
        await _sqlConnection.ExecuteAsync("StoreReservationRequest", parameters, commandType: CommandType.StoredProcedure);
        var reservationId = parameters.Get<Guid>("@ReservationId");
        return reservationId;
    }

    private ValidationResult Validate(ReservationRequestMessage message, out IReadOnlyCollection<string> errors)
    {
        DateTime dateResult;
        var rules = new List<Expression<Func<ReservationRequestMessage, bool>>>
        {
            m => !string.IsNullOrWhiteSpace(m.ClientName),
            m => m.ClientTelephone.All(char.IsDigit) && m.ClientTelephone.Length == 10,
            m => m.NumberOfReservedTable >= 0,
            m => DateTime.TryParseExact(m.DateOfReservation, "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dateResult)
        };
        var ruleResults = rules.Select(rule => (Rule: rule, Result: rule.Compile()(message)));
        var failedRules = ruleResults.Where(x => x.Result == false).ToArray();
        bool valid = !failedRules.Any();
        errors = failedRules.Select(x => x.Rule.ToString() ?? string.Empty).ToList().AsReadOnly();

        return valid ? ValidationResult.Ok : ValidationResult.Fail;
    }
}