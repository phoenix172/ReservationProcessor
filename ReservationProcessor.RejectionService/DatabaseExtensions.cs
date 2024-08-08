using System.Data;
using Dapper;
using Npgsql;

namespace ReservationProcessor.ReservationService;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabase(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await using var masterConnection = scope.ServiceProvider.GetRequiredKeyedService<NpgsqlConnection>("PostgresDefaultDB");

        var exists = await masterConnection.QuerySingleAsync<int>("SELECT COUNT(*) FROM pg_database WHERE datname = 'RejectionsDB'") > 0;

        if(!exists)
            await masterConnection.ExecuteAsync("CREATE DATABASE RejectionsDB");

        await using var rejectionsConnection = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

        await rejectionsConnection.ExecuteAsync(
            """
              CREATE TABLE IF NOT EXISTS RejectedReservations(
                  Id uuid NOT NULL PRIMARY KEY,
                  ReservationId uuid NOT NULL,
                  ValidationDate timestamp NOT NULL,
                  Errors varchar NOT NULL
              )
            """);

        await rejectionsConnection.ExecuteAsync(
            """
            CREATE OR REPLACE PROCEDURE public."StoreReservationRejection"(
            	IN reservationid uuid,
            	IN validationdate timestamp,
            	IN errors character varying,
            	OUT rejectionid uuid)
            LANGUAGE 'sql'
            AS $BODY$
            	
            INSERT INTO rejectedreservations (id, reservationid, validationdate, errors)
            VALUES (gen_random_uuid(), reservationid, validationdate, errors)
            	RETURNING id as rejectionid
            
            $BODY$;
            """);

    }
}