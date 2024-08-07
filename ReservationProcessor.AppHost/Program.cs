using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", false);
var password = builder.AddParameter("password", true);

var messaging = builder.AddRabbitMQ("MessageBus", username, password, 16081).WithManagementPlugin(15672);
var mssql = builder.AddSqlServer("MSSQL", password, 16082).WithHealthCheck();
var postgres = builder.AddPostgres("Postgres",username, password, 16083).WithHealthCheck();

var reservationsDatabase = mssql.AddDatabase("ReservationsDB");
var masterDatabase = mssql.AddDatabase("MasterDB", "master");
var rejectionsDatabase = postgres.AddDatabase("RejectionsDatabase");

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
    .WithReference(reservationsDatabase)
    .WaitFor(reservationsDatabase)
    .WaitFor(messaging);

builder.AddProject<ReservationProcessor_RejectionService>("RejectionService")
    .WithReference(messaging)
    .WithReference(rejectionsDatabase)
    .WaitFor(rejectionsDatabase)
    .WaitFor(messaging);

builder.Build().Run();
