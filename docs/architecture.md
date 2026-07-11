# Architecture

The template is a deliberately small service, not a framework. It provides production-oriented boundaries while leaving feature-specific policy to the generated service.

## Shape

```text
src/
  MicroserviceTemplate/
    Features/Tasks/
      Create/          operation contract, route, and behavior
      Get/
      List/
      Update/
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

New behavior should normally be added as an operation inside its feature. Avoid horizontal handler/service/repository layers unless they hide substantial complexity or enable a real boundary.

## Defaults and boundaries

- ASP.NET Core Minimal APIs with built-in validation and OpenAPI.
- PostgreSQL through EF Core and Aspire's Npgsql integration.
- Optimistic concurrency through an explicit version token.
- RFC 9457 Problem Details with request and trace correlation.
- OpenTelemetry logs, traces, and metrics; structured console logs in production.
- Standard HTTP resilience with retries disabled for unsafe methods.
- Readiness at `/health` and liveness at `/alive`.
- Aspire for local orchestration and service discovery.

Redis, messaging, authentication, authorization, and cloud-vendor SDKs are intentionally absent. Add them only for a concrete service requirement. Authentication in particular needs an explicit issuer, audience, authorization model, and operational owner.

The previous process-local rate limiter is also intentionally absent: it creates inconsistent limits when replicas scale out. Decide whether limits belong at the cloud ingress/API gateway or in the service, then add a distributed design and explicit policy tests.

## Task reference feature

Tasks demonstrate validation, pagination, domain transitions, UUID v7 identifiers, injected time, EF projections, optimistic concurrency, telemetry, and endpoint-level integration testing. It is reference code: replace it with the first real feature, keeping the organizational principles rather than the mock terminology.
