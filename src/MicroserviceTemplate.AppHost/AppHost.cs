var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

builder.AddProject<Projects.MicroserviceTemplate>("apiservice", launchProfileName: "http")
    .WithReference(cache).WaitFor(cache)
    .WithReference(postgresdb).WaitFor(postgresdb)
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.Build().Run();
