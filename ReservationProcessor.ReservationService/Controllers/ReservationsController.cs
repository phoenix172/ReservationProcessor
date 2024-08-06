using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ReservationProcessor.ReservationService.Controllers;

[ApiController]
[Route("[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ILogger<ReservationsController> _logger;
    private readonly SqlConnection _connection;

    public ReservationsController(ILogger<ReservationsController> logger, SqlConnection connection)
    {
        _logger = logger;
        _connection = connection;
    }

    [HttpPost]
    public async Task<IActionResult> Store([FromBody]ReservationRequestMessage message)
    {
        var request = new ReservationRequest()
        {
            RawRequest = JsonSerializer.Serialize(message),
            DateCreated = DateTime.Now,
            ValidationResult = null
        };
        await _connection.ExecuteAsync("StoreReservationRequest", request, commandType: CommandType.StoredProcedure);

        return Ok(_connection.Database);
    }
}

public class ReservationRequest
{
    public string RawRequest { get; set; }
    public DateTime DateCreated { get; set; }
    public ValidationResult? ValidationResult { get; set; }
}

public enum ValidationResult
{
    Fail=0,
    Ok=9
}