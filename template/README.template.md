# MicroserviceTemplate

A compact .NET 10 microservice with Aspire orchestration, PostgreSQL, vertical slices, and production-aware defaults.

## Start locally

Prerequisites are the .NET 10 SDK, the Aspire CLI, and Docker.

```bash
dotnet tool restore
aspire start --non-interactive
```

Use the Aspire dashboard to open the API and Scalar UI, inspect telemetry and health, or run the `seed-tasks` command. The API exposes readiness at `/health` and liveness at `/alive`.

## Structure

```text
src/
  MicroserviceTemplate/
    Features/Tasks/
      Create/ Get/ List/ Update/ Complete/ Delete/
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

The Tasks sample demonstrates validation, bounded paging, UUID v7 identifiers, `TimeProvider`, state transitions, optimistic concurrency, EF projections, Problem Details, telemetry, and focused tests. Replace it with your first real feature while retaining the shape.

See [architecture](docs/architecture.md), [operations](docs/operations.md), and [agent guidance](AGENTS.md).

## Included defaults

- Minimal APIs, built-in validation, OpenAPI, and Scalar in Development
- EF Core and PostgreSQL through Aspire
- RFC 9457 Problem Details with trace and request correlation
- structured console logging and OpenTelemetry logs, metrics, and traces
- service discovery and HTTP resilience without unsafe-method retries
- unit tests plus Aspire-hosted integration tests
- generated CI, central package management, analyzers, and local EF tooling

Redis, messaging, and cloud-provider SDKs are deliberately optional. Authentication and authorization are also not configured: select an identity provider, issuer, audience, and policy model for the real service before exposing protected behavior.

Rate limiting is an explicit deployment decision. Prefer an ingress/API-gateway policy or add a distributed service policy; a process-local default becomes inconsistent as replicas scale.

## Verify

```bash
dotnet build -c Release
dotnet test --solution MicroserviceTemplate.slnx -c Release --no-build
dotnet ef migrations has-pending-model-changes --project src/MicroserviceTemplate/MicroserviceTemplate.csproj --startup-project src/MicroserviceTemplate/MicroserviceTemplate.csproj --no-build --configuration Release
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release --no-build
```

Integration tests require Docker. Database migrations run automatically only in Development; use a separate deployment step and privileged identity in production.
