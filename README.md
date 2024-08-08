A sample microservice architecture consisting of three .NET 8 microservices, communicating using RabbitMQ:
ReservationService, backed by MSSQL,
AcceptanceService, backed by MSSQL
FailureService, backed by Postgres

Run using .Net Aspire or by using the provided docker-compose file in /ReservationProcessor.AppHost/docker-compose-deps.yaml
If using docker-compose, you will need to configure appsettings.json for each of the projects in their respective directories
Connection strings for each dependency(RabbitMQ, MSSQL and Postgres) need to be configured.