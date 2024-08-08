using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", false);
var password = builder.AddParameter("password", true);

var messaging = builder.AddRabbitMQ("MessageBus", username, password, 16081).WithManagementPlugin(15672).WithHealthCheck();
var mssql = builder.AddSqlServer("MSSQL", password, 16082).WithHealthCheck();
var postgres = builder.AddPostgres("Postgres",username, password, 16083).WithPgAdmin().WithHealthCheck();

var reservationsDatabase = mssql.AddDatabase("ReservationsDB");
var acceptanceDatabase = mssql.AddDatabase("AcceptanceDB");
var masterDatabase = mssql.AddDatabase("MasterDB", "master");
var postgresDefaultDatabase = postgres.AddDatabase("PostgresDefaultDB","postgres");
var rejectionsDatabase = postgres.AddDatabase("RejectionsDB".ToLowerInvariant());

builder
    .AddProject<ReservationProcessor_ReservationService>("ReservationService")
    .WithReference(messaging)
    .WithReference(reservationsDatabase)
    .WithReference(masterDatabase)
    .WaitFor(masterDatabase)
    .WaitFor(messaging);

builder
    .AddProject<ReservationProcessor_AcceptanceService>("AcceptanceService")
    .WithReference(messaging)
    .WithReference(acceptanceDatabase)
    .WithReference(masterDatabase)
    .WaitFor(masterDatabase)
    .WaitFor(acceptanceDatabase)
    .WaitFor(messaging);

builder.AddProject<ReservationProcessor_RejectionService>("RejectionService")
    .WithReference(messaging)
    .WithReference(rejectionsDatabase)
    .WithReference(postgresDefaultDatabase)
    .WaitFor(rejectionsDatabase)
    .WaitFor(postgresDefaultDatabase)
    .WaitFor(messaging);

builder.Build().Run();
