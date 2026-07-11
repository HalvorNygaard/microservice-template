var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

var api = builder.AddProject<Projects.MicroserviceTemplate>("apiservice", launchProfileName: "http")
    .WithReference(postgresdb).WaitFor(postgresdb)
    .WithHttpHealthCheck("/health", endpointName: "http");

api.WithSeedTasksCommand();

builder.Build().Run();
