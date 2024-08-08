using Dapper;
using Microsoft.Data.SqlClient;

namespace ReservationProcessor.AcceptanceService;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabase(this IHost app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await using var masterConnection = scope.ServiceProvider.GetRequiredKeyedService<SqlConnection>("MasterDB");

        await masterConnection.ExecuteAsync(
            """
            IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'AcceptanceDB')
            BEGIN
                CREATE DATABASE AcceptanceDB
            END
            """);

        await using var acceptanceConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

        await acceptanceConnection.ExecuteAsync(
            """
            IF NOT EXISTS(SELECT * FROM sys.tables WHERE name = 'AcceptedRequests')
            BEGIN
              CREATE TABLE AcceptedRequests(
                  Id uniqueidentifier NOT NULL PRIMARY KEY,
                  ReservationId uniqueidentifier NOT NULL,
                  ValidationDate datetime2 NOT NULL
              )
            END
            """);

        await acceptanceConnection.ExecuteAsync(
            """
            IF EXISTS (
               SELECT type_desc, type
               FROM sys.procedures WITH(NOLOCK)
               WHERE NAME = 'StoreAcceptedRequest'
                   AND type = 'P'
             )
            DROP PROCEDURE dbo.StoreAcceptedRequest
            """);
        await acceptanceConnection.ExecuteAsync(
            """
            CREATE PROCEDURE StoreAcceptedRequest
                (@ValidationDate datetime2,
                @ReservationId uniqueidentifier,
                @AcceptanceId uniqueidentifier OUT)
            AS
            BEGIN
                DECLARE @InsertResult table (Id uniqueidentifier)
                INSERT INTO AcceptedRequests 
                OUTPUT INSERTED.Id into @InsertResult
                VALUES (NEWID(), @ReservationId, @ValidationDate)
                SET @AcceptanceId = (select Id from @InsertResult)
                RETURN 0
            END
            """);

    }
}