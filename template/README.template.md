# MicroserviceTemplate

A minimal .NET 10 microservice generated from the Modern Microservice Template.

## Run

This project uses Aspire for local infrastructure. Start the AppHost:

```bash
dotnet run --project src/MicroserviceTemplate.AppHost/MicroserviceTemplate.AppHost.csproj
```

The API is exposed through the AppHost on localhost.

Scalar/OpenAPI are enabled in Development.

In Development, the app also applies EF Core migrations during startup. The development-only Scalar/OpenAPI and root redirect setup lives in `Configurations/Setup/DevelopmentSetup.cs`.

## Development

Restore and run:

```bash
dotnet restore
dotnet run --project src/MicroserviceTemplate/MicroserviceTemplate.csproj
```

Run the tests:

```bash
dotnet test
```

Integration tests require Docker because the Aspire test host starts PostgreSQL and Redis.

## Features

Application features live under:

```text
src/MicroserviceTemplate/Features/
```

Each feature owns its models, endpoints, operation request/response contracts, handlers, feature services, cache usage, and DI registration.

The starter Tasks sample separates feature registration from endpoint mapping:

```text
src/MicroserviceTemplate/Features/Tasks/TaskFeature.cs
src/MicroserviceTemplate/Features/Tasks/TaskEndpoints.cs
src/MicroserviceTemplate/Features/Tasks/TaskObservability.cs
```

`TaskFeature.cs` registers handlers and services. `TaskEndpoints.cs` maps `/api/tasks` and applies `RequireRateLimiting(RateLimitingSetup.ApiPolicyName)`. `TaskObservability.cs` contains structured `LoggerMessage` methods and task metrics.

Request validation is registered by default. Put data annotation attributes directly on operation request records, such as `CreateTaskRequest` or `UpdateTaskRequest`.

List endpoints should be bounded. The starter task list returns `PagedResult<T>` and clamps `pageSize` to avoid unbounded reads.

Use `ApplicationProblemException` for reusable expected failures that should be converted to ProblemDetails by the global exception handler.

Strongly typed configuration options live under `Configurations/Options`, and app startup wiring lives under `Configurations/Setup`. Cache TTLs are configured from the `Cache` section in `appsettings.json`.

Shared code lives under:

```text
src/MicroserviceTemplate/Common/
src/MicroserviceTemplate/Configurations/
src/MicroserviceTemplate/Infrastructure/Data/
src/MicroserviceTemplate/Program.cs
```

`Common/MicroserviceTelemetry.cs` provides the service meter, activity source, common tags, and activity helpers. `Common/Http` contains global exception handling plus exception metrics and activity enrichment.

## Infrastructure

The AppHost configures:

- PostgreSQL for EF Core
- Redis for distributed caching
- OpenTelemetry collection
- Health and liveness checks

The API project uses resilient HTTP defaults and service discovery when HTTP clients are created.

Health endpoints are exposed at `/health` for readiness and `/alive` for liveness.

OpenTelemetry logs, metrics, and traces include service resource attributes. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to enable OTLP export, and tune tracing with `OpenTelemetry:Tracing:SamplingRatio`.

API routes use a starter rate-limit policy named `api`. Tune it from `RateLimiting:Api` in `appsettings.json`, or apply the policy to new route groups with `RequireRateLimiting(RateLimitingSetup.ApiPolicyName)`.

## EF Core migrations

Run these commands from the API project directory:

```bash
dotnet ef migrations add AddFeatureName
dotnet ef database update
```

## Tests

```bash
dotnet test --project tests/MicroserviceTemplate.IntegrationTests/MicroserviceTemplate.IntegrationTests.csproj
```

Integration tests use Aspire testing to start PostgreSQL, Redis, and the API. Template validation in the source repository also verifies generated projects build, replace template tokens, and run their generated integration tests.
