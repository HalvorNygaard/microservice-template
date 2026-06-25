# MicroserviceTemplate

A minimal .NET 10 microservice generated from the Modern Microservice Template.

## Run

This project uses Aspire for local infrastructure. Start the AppHost:

```bash
dotnet run --project src/MicroserviceTemplate.AppHost/MicroserviceTemplate.AppHost.csproj
```

The API is exposed through the AppHost on localhost.

Scalar/OpenAPI are enabled in Development.

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

Request validation is registered by default. Put data annotation attributes directly on operation request records, such as `CreateTaskRequest` or `UpdateTaskRequest`.

Strongly typed configuration options live under `Configurations/Options`, and app startup wiring lives under `Configurations/Setup`. Cache TTLs are configured from the `Cache` section in `appsettings.json`.

Shared code lives under:

```text
src/MicroserviceTemplate/Common/
src/MicroserviceTemplate/Configurations/
src/MicroserviceTemplate/Infrastructure/Data/
src/MicroserviceTemplate/Program.cs
```

## Infrastructure

The AppHost configures:

- PostgreSQL for EF Core
- Redis for distributed caching
- OpenTelemetry collection
- Health and liveness checks

The API project uses resilient HTTP defaults and service discovery when HTTP clients are created.

API routes use a starter rate-limit policy named `api`. Tune it from `RateLimiting:Api` in `appsettings.json`, or apply the policy to new route groups with `RequireRateLimiting(RateLimitingSetup.ApiPolicyName)`.

## EF Core migrations

Run these commands from the API project directory:

```bash
dotnet ef migrations add AddFeatureName
dotnet ef database update
```
