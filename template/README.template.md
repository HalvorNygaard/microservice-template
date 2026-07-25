# MicroserviceTemplate

A PostgreSQL vertical-slice reference service built with .NET 10 and Aspire.

## Start locally

Prerequisites are the .NET 10 SDK, the Aspire CLI, and Docker.

```bash
dotnet tool restore
aspire start --non-interactive
```

Use the Aspire dashboard to open the API and Scalar UI and inspect PostgreSQL, telemetry, and health. The API exposes readiness at `/health` and liveness at `/alive`.

The supplied service name remains the project, assembly, path, container, and Aspire resource identity. C# namespaces are derived separately as clean PascalCase segments: a name such as `ms-edi` becomes `MsEdi`, while a dotted name retains its namespace segments.

## Structure

```text
src/
  MicroserviceTemplate/
    Features/Tasks/
      Create/ Get/ Complete/ Delete/
      Internal/
      TaskItem.cs
      TasksFeature.cs
    Common/
    Configurations/
    Infrastructure/Data/
  MicroserviceTemplate.AppHost/
tests/
  MicroserviceTemplate.UnitTests/
  MicroserviceTemplate.IntegrationTests/
```

An operation folder owns its route, request contract, result types, and behavior. Domain invariants stay with the feature's domain type. Shared folders are for cross-cutting behavior, not a default dumping ground.

Tasks is intentionally small reference code. It demonstrates validation, UUID v7 identifiers, `TimeProvider`, a state change, EF Core mapping and projections, Problem Details, telemetry, and focused tests. Replace it with the first real feature while retaining the organization.

Start with the removable [greenfield checklist](docs/greenfield.md), then see [architecture](docs/architecture.md), [operations](docs/operations.md), and [agent guidance](AGENTS.md).

## Included defaults

- Minimal APIs, built-in validation, OpenAPI, and Scalar in Development
- EF Core and PostgreSQL through Aspire, with automatic database retries disabled until writes are designed for them
- Problem Details with stable codes and request/trace correlation
- bounded API and health-check request timeouts
- structured console logging and OpenTelemetry logs, metrics, and traces
- unit tests plus Aspire-hosted integration tests
- a contained GitHub Actions workflow, central package management, analyzers, and local EF tooling

Redis, messaging, outbound resilience, and cloud-provider SDKs are optional. Authentication and authorization are not configured: select an identity provider, issuer, audience, and policy model for the real service before exposing protected behavior.

## Verify

```bash
dotnet tool restore
dotnet build -c Release
dotnet test --project tests/MicroserviceTemplate.UnitTests/MicroserviceTemplate.UnitTests.csproj -c Release --no-build
dotnet test --project tests/MicroserviceTemplate.IntegrationTests/MicroserviceTemplate.IntegrationTests.csproj -c Release --no-build
dotnet ef migrations has-pending-model-changes --project src/MicroserviceTemplate/MicroserviceTemplate.csproj --startup-project src/MicroserviceTemplate/MicroserviceTemplate.csproj --no-build --configuration Release
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release --no-build
```

Integration tests require Docker. Database migrations run automatically only in Development; use a separate deployment step and privileged identity in production.
