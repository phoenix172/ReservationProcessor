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

        await using var reservationsConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

        await InitializeReservations(reservationsConnection);
        await InitializeResponses(reservationsConnection);
    }

    private static async Task InitializeReservations(SqlConnection reservationsConnection)
    {
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
                @ValidationResult int,
                @ReservationId uniqueIdentifier OUT)
            AS
            BEGIN
                DECLARE @InsertResult table (Id uniqueidentifier)
                INSERT INTO ReservationRequests
                OUTPUT INSERTED.Id into @InsertResult
                VALUES (NEWID(), @RawRequest, @DateCreated, @ValidationResult)
                SET @ReservationId = (select Id from @InsertResult)
                RETURN 0
            END
            """);
    }

    private static async Task InitializeResponses(SqlConnection reservationsConnection)
    {
        await reservationsConnection.ExecuteAsync(
            """
            IF NOT EXISTS(SELECT * FROM sys.tables WHERE name = 'ReservationSuccessResponses')
            BEGIN
              CREATE TABLE ReservationSuccessResponses(
                  Id uniqueidentifier NOT NULL PRIMARY KEY,
                  ReservationId uniqueidentifier,
                  ValidationDate datetime2 NOT NULL,
                  AcceptanceId uniqueidentifier NOT NULL
              )
            END
            """);

        await reservationsConnection.ExecuteAsync(
            """
            IF EXISTS (
               SELECT type_desc, type
               FROM sys.procedures WITH(NOLOCK)
               WHERE NAME = 'StoreSuccessResponse'
                   AND type = 'P'
             )
            DROP PROCEDURE dbo.StoreSuccessResponse
            """);

        await reservationsConnection.ExecuteAsync(
            """
            CREATE PROCEDURE StoreSuccessResponse
                (@ReservationId uniqueidentifier,
                @ValidationDate datetime2,
                @ValidationSuccessId uniqueIdentifier,
                @ResponseId uniqueidentifier OUT)
            AS
            BEGIN
                DECLARE @InsertResult table (Id uniqueidentifier)
                INSERT INTO ReservationSuccessResponses
                OUTPUT INSERTED.Id into @InsertResult
                VALUES (NEWID(), @ReservationId, @ValidationDate, @ValidationSuccessId)
                SET @ResponseId = (select Id from @InsertResult)
                RETURN 0
            END
            """);
    }
}