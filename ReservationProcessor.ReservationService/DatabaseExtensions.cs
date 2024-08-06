using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;

namespace ReservationProcessor.ReservationService;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabase(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await using var masterConnection = scope.ServiceProvider.GetRequiredKeyedService<SqlConnection>("MasterDB");

        await masterConnection.ExecuteAsync(
            """
            IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'ReservationsDB')
            BEGIN
                CREATE DATABASE ReservationsDB
            END
            """);

        await using var reservationsConnection = scope.ServiceProvider.GetService<SqlConnection>();

        await reservationsConnection.ExecuteAsync(
            """
            IF NOT EXISTS(SELECT * FROM sys.tables WHERE name = 'ReservationRequests')
            BEGIN
              CREATE TABLE ReservationRequests(
                  Id uniqueidentifier NOT NULL PRIMARY KEY,
                  Raw_Request varchar(max),
                  DT datetime2 NOT NULL,
                  Validation_result int NOT NULL
              )
            END
            """);

        await reservationsConnection.ExecuteAsync(
            """
            IF EXISTS (
               SELECT type_desc, type
               FROM sys.procedures WITH(NOLOCK)
               WHERE NAME = 'StoreReservationRequest'
                   AND type = 'P'
             )
            DROP PROCEDURE dbo.StoreReservationRequest
            """);
        await reservationsConnection.ExecuteAsync(
            """
            CREATE PROCEDURE StoreReservationRequest
                (@RawRequest varchar(max),
                @DateCreated datetime2,
                @ValidationResult int)
            AS
            BEGIN
                INSERT INTO ReservationRequests VALUES (NEWID(), @RawRequest, @DateCreated, @ValidationResult)
            END
            """);

    }
}