# Architecture

This is a PostgreSQL vertical-slice reference service, not an application framework. It provides a small production-aware baseline while leaving domain and infrastructure policy to the generated service.

## Shape

```text
src/
  MicroserviceTemplate/
    Features/Tasks/
      Create/          operation contract, route, and behavior
      Get/
      Complete/
      Delete/
      Internal/        persistence details owned by the feature
      TaskItem.cs      domain state and invariants
      TasksFeature.cs  feature route composition
    Common/            genuinely shared HTTP and telemetry primitives
    Configurations/    application-hosting composition
    Infrastructure/    database context and migrations
  MicroserviceTemplate.AppHost/
tests/
  MicroserviceTemplate.UnitTests/
  MicroserviceTemplate.IntegrationTests/
```

Add new behavior as an operation inside its feature. Introduce shared handlers, services, repositories, or other layers only when they hide meaningful complexity or form a real boundary.

## Included boundaries

- ASP.NET Core Minimal APIs with built-in validation and OpenAPI.
- PostgreSQL through EF Core and Aspire's Npgsql integration. Automatic EF execution-strategy retries are disabled until a service deliberately designs idempotent writes and commit-ambiguity handling.
- Problem Details with stable codes and request/trace correlation.
- OpenTelemetry logs, traces, and metrics configured through standard environment variables.
- One API timeout policy and one health-check timeout policy.
- Readiness at `/health` and liveness at `/alive`.
- Aspire for local orchestration.

Redis, messaging, authentication, authorization, outbound resilience, rate limiting, and cloud-vendor SDKs are intentionally absent. Add them only for a concrete requirement and document the resulting operational ownership.

## Task reference feature

Tasks demonstrates validation, a simple state change, UUID v7 identifiers, injected time, EF projections, telemetry, and endpoint-level integration testing. Replace it with the first real feature, keeping the organizational principles rather than the sample terminology.
