var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

builder.AddProject<Projects.MicroserviceTemplate>("apiservice")
    .WithReference(cache).WaitFor(cache)
    .WithReference(postgresdb).WaitFor(postgresdb)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
