# Modern Microservice Template

A PostgreSQL vertical-slice reference service built with .NET 10 and Aspire. It favors feature colocation, low abstraction, and built-in .NET capabilities over framework layers and preselected infrastructure.

## What it generates

- Minimal API service and Aspire AppHost
- PostgreSQL with EF Core migrations
- operation-colocated vertical slices
- Problem Details, validation, API and health-check timeouts, health checks, structured logging, and OpenTelemetry
- unit and Aspire-hosted integration tests
- central package management, analyzers, local EF tooling, and a small GitHub Actions workflow

The Tasks reference demonstrates Create, Get, Complete, and Delete operations, UUID v7 identifiers, `TimeProvider`, EF Core mapping and projections, telemetry, and focused tests. Redis, messaging, authentication, authorization, outbound resilience, and vendor-specific deployment libraries stay out until a service has a concrete need.

## Use the template

Prerequisites: .NET 10 SDK, Aspire CLI, and Docker.

```bash
dotnet pack -c Release template/microservice-template.Template.csproj
dotnet new install ./.artifacts/package/release/ModernMicroservice.Template.*.nupkg
dotnet new modern-microservice -n MyService
cd MyService
dotnet tool restore
aspire start --non-interactive
```

The supplied name remains the project, assembly, path, container, and Aspire resource identity. C# namespaces are derived separately as clean PascalCase segments: `ms-edi` becomes `MsEdi`, while `Example.Service` remains `Example.Service`.

Use the Aspire dashboard to open the API and Scalar UI and inspect PostgreSQL, telemetry, and health. Scalar/OpenAPI and automatic migration application are Development-only.

## Generated design

```text
src/<ServiceName>/Features/Tasks/
  Create/ Get/ Complete/ Delete/
  Internal/
  TaskItem.cs
  TaskRepresentation.cs
  TasksFeature.cs
```

Each operation file owns its route mapping, contract, response shape, and behavior. The domain type owns state changes and invariants. Cross-cutting composition remains in `Common`, `Configurations`, and `Infrastructure`; `Program.cs` stays small.

The generated project includes durable architecture and operations notes, agent guidance, and a removable `docs/greenfield.md` checklist for replacing Tasks with the first real domain slice.

## Verify this repository

```bash
dotnet tool restore
dotnet build MicroserviceTemplate.slnx -c Release
dotnet test --project tests/MicroserviceTemplate.UnitTests/MicroserviceTemplate.UnitTests.csproj -c Release --no-build
dotnet test --project tests/MicroserviceTemplate.IntegrationTests/MicroserviceTemplate.IntegrationTests.csproj -c Release --no-build
dotnet test --project tests/TemplateValidation.Tests/TemplateValidation.Tests.csproj -c Release
```

Integration and template-validation tests require Docker. Template validation packs and installs the template, generates and builds services with PascalCase, dotted, and hyphenated names, verifies structure and substitutions, restores tools, checks migration/model parity, publishes the primary service, and runs its unit and integration tests.

## Package publishing

The `Release package` workflow validates and publishes a selected version to GitHub Packages. For a local package:

```bash
dotnet pack -c Release template/microservice-template.Template.csproj
```

## License

MIT
