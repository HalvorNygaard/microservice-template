var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var postgresdb = postgres.AddDatabase("postgresdb");

builder.AddProject(
        "apiservice",
        "../MicroserviceTemplate/MicroserviceTemplate.csproj",
        launchProfileName: "http")
    .WithReference(postgresdb).WaitFor(postgresdb)
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar";
        url.Url = "/scalar/v1";
    });

builder.Build().Run();
