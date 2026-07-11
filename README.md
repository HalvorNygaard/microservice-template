# Modern Microservice Template

A focused .NET 10 template for producing consistent, cloud-ready microservices with little ceremony. It favors feature colocation and built-in .NET capabilities over framework layers and preselected infrastructure.

## What it generates

- Minimal API service and Aspire AppHost
- PostgreSQL with EF Core migrations
- operation-colocated vertical slices
- Problem Details, validation, health checks, structured logging, and OpenTelemetry
- safe HTTP resilience defaults and service discovery
- unit and Aspire-hosted integration tests
- local EF tooling, migration drift checks, publish validation, generated CI, and agent guidance

The reference Tasks feature covers CRUD, completion transitions, bounded pagination, UUID v7 IDs, injected time, optimistic concurrency, telemetry, and API tests. Redis, messaging, authentication, authorization, and vendor-specific deployment libraries stay out until a service has a concrete need.

## Use the template

Prerequisites: .NET 10 SDK, Aspire CLI, and Docker.

```bash
dotnet pack -c Release template/microservice-template.Template.csproj
dotnet new install ./template/bin/Release/ModernMicroservice.Template.*.nupkg
dotnet new modern-microservice -n MyService
cd MyService
dotnet tool restore
aspire start --non-interactive
```

The dashboard exposes the API, PostgreSQL, telemetry, health, and a `seed-tasks` command. Scalar/OpenAPI and automatic migration application are Development-only.

## Generated design

```text
src/<ServiceName>/Features/Tasks/
  Create/ Get/ List/ Update/ Complete/ Delete/
  Internal/
  TaskItem.cs
  TaskRepresentation.cs
  TasksFeature.cs
```

Each operation file owns its route mapping, contract, response shape, and behavior. The domain type owns transitions and invariants. Cross-cutting composition remains in `Common`, `Configurations`, and `Infrastructure`; `Program.cs` stays small.

The generated project includes `docs/architecture.md`, `docs/operations.md`, and `AGENTS.md` so future agents have the same boundaries and verification contract.

## Verify this repository

```bash
dotnet tool restore
dotnet build MicroserviceTemplate.slnx -c Release
dotnet test --solution MicroserviceTemplate.slnx -c Release --no-build
dotnet test --project tests/TemplateValidation.Tests/TemplateValidation.Tests.csproj -c Release
```

Integration and template-validation tests require Docker. Template validation packs and installs the template, generates a renamed service, checks its structure and substitutions, restores its tools, verifies migration/model parity, builds and publishes it, and runs its tests.

## Package publishing

The `Release package` workflow validates and publishes a selected version to GitHub Packages. For a local package:

```bash
dotnet pack -c Release template/microservice-template.Template.csproj
```

## License

MIT
