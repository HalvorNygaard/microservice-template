# Vision

This template is a starting point for small, modern, production-aware .NET services.

The goal is not to provide a framework, a domain architecture, or a large application scaffold. The goal is to provide a clean default service that feels like something a .NET team would want to maintain: minimal, explicit, testable, observable, and easy to extend.

## Core idea

A microservice should be easy to understand from the outside in.

When someone opens the project, they should quickly see:

- where the service starts
- what local infrastructure it uses
- where features live
- how to add a new behavior
- how to run and test it
- what defaults are already wired

The template avoids hiding important behavior behind generated abstractions or deep project hierarchies. Most of the service is plain .NET, Minimal APIs, EF Core, Aspire, and Microsoft.Extensions libraries.

## Minimal by default

The template should feel small.

It includes defaults that are commonly useful for a real service, but it avoids defaults that imply a specific product direction:

- no authentication scheme
- no authorization policy
- no multi-project domain/application/infrastructure split
- no repository abstraction
- no custom mediator library
- no pagination contract
- no output caching
- no request timeouts
- no artificial service layer
- no framework-like base classes
- no custom feature telemetry

The service starts with one API project, one AppHost, one feature, EF Core, Redis, PostgreSQL, OpenTelemetry, health checks, and tests.

If a service needs more, the team adds it intentionally.

## Locality of behavior

The most important architectural idea is locality of behavior.

Code that changes together should live together. A feature should own the pieces needed to implement its behavior:

- endpoint mapping
- request DTOs
- response DTOs
- command or query handlers
- entity mapping
- cache usage
- dependency injection registration

This is why the template uses vertical slices under `Features/` instead of splitting everything into horizontal layers like Controllers, Services, Repositories, and DTO folders.

Horizontal folders still exist, but only for code that is truly cross-cutting:

- `Common/` for small shared abstractions and problem-details handling
- `Data/` for EF Core setup
- `MicroserviceSetup.cs` for service-wide defaults and OpenTelemetry configuration
- `Program.cs` as the composition root

The rule is simple: if code is only used by one feature, keep it in that feature.

## Modern .NET first

The template should use modern .NET and C# when it improves clarity without adding ceremony.

Current examples include:

- .NET 10
- Minimal APIs
- primary constructors where they reduce noise
- collection expressions where they are clearer
- record DTOs with validation attributes
- `DateTimeOffset` for API timestamps
- string enum JSON serialization
- central package management
- shared analyzers
- `.editorconfig`
- `.slnx`
- Aspire resource orchestration
- OpenTelemetry signals
- health checks and Problem Details

The template should not use a feature just because it is new. It should use modern features when they make the code easier to read, safer, or more idiomatic.

## Operational by default

A service should be observable and diagnosable from the start.

The template wires practical operational defaults:

- Aspire dashboard integration
- OpenTelemetry logs, traces, and metrics
- health and liveness endpoints
- SimpleConsole logging tuned for local development
- OTLP export when configured
- HTTP resilience through Microsoft.Extensions.Http.Resilience
- service discovery hooks
- cache invalidation near the feature that uses the cache

These defaults should help a team run the service locally and understand it in a distributed environment without forcing a specific cloud provider.

## Testable without being heavyweight

The template includes integration tests that run the real service through HTTP using Aspire testing.

The goal is to test behavior, not implementation details. The tests should answer questions like:

- can the service start with its resources?
- can it create, read, update, and delete a task?
- does invalid input return the expected response?
- does the generated project build and run?

The template also includes validation tests for the template package itself. A template that generates broken projects is not useful, even if the source project builds.

## Keep the escape hatches visible

The template should not make teams fight the defaults.

If a team needs authentication, multi-tenancy, pagination, outbox patterns, Sagas, gRPC, background workers, or a different database, they should be able to add those pieces without unwinding the template.

The defaults are a baseline, not a cage.

## Non-goals

This template is not trying to be:

- a full enterprise architecture
- a DDD reference implementation
- a SaaS starter kit
- a multi-service platform
- a replacement for Aspire
- a framework on top of Minimal APIs
- a code generator
- a heavily abstracted clean architecture template

It is a small service template with good defaults and a clear path for extension.

## Design tension

The template lives between two extremes:

- too empty to be useful
- too opinionated to adapt

The intended balance is:

- include the boring-but-important defaults
- keep the code obvious
- avoid abstractions that do not earn their place
- prefer .NET and C# features over custom infrastructure
- make behavior easy to find and change

If a piece of code cannot explain why it belongs in the template, it probably should not be there.

## Future direction

Good future improvements should preserve the vision:

- add more tests that prove generated projects work
- improve EF migration ergonomics without adding ceremony
- keep analyzers useful but not noisy
- prefer Microsoft.Extensions and Aspire APIs over custom packages
- document decisions near the code
- remove defaults that are not broadly useful
- keep the first feature easy to copy and reshape

The template should stay small enough that a developer can understand it in one sitting, but complete enough that a real service can start from it with confidence.
