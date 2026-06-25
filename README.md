# Modern Microservice Template

A minimal .NET 10 template for creating Aspire-backed microservices with a feature-sliced Minimal API structure.

It generates a small, production-aware service that is still easy to understand in one sitting:

- one API project
- one Aspire AppHost project
- PostgreSQL and Redis resources
- EF Core with migrations
- OpenTelemetry, health and liveness endpoints, and resilient HTTP defaults
- Scalar/OpenAPI in development
- TUnit integration tests using Aspire testing
- central package management, analyzers, `.editorconfig`, and Microsoft Testing Platform

## Prerequisites

- .NET 10 SDK
- Docker, for Aspire resources and integration tests
- Git, if you want to clone and customize the template

## Quick Start

Pack and install the template locally:

```bash
dotnet pack -c Release template/microservice-template.Template.csproj

dotnet new install ./template/bin/Release/ModernMicroservice.Template.*.nupkg

dotnet new modern-microservice -n MyService

cd MyService

dotnet run --project src/MyService.AppHost/MyService.AppHost.csproj
```

The AppHost starts the API plus PostgreSQL and Redis. In development, OpenAPI is exposed through Scalar.

Uninstall the local template when you are done testing it:

```bash
dotnet new uninstall ModernMicroservice.Template
```

## Generated Structure

A generated service uses `<ServiceName>` as the root namespace and project name.

```text
src/
  <ServiceName>/
  <ServiceName>.AppHost/
tests/
  <ServiceName>.IntegrationTests/
```

Application behavior lives in feature folders under `Features/`. Cross-cutting service defaults live in `Common/`, `Configurations/`, `Infrastructure/Data/`, and `Program.cs`.

The starter task feature uses this shape:

```text
Features/
  Tasks/
    Models/
    Operations/
      Create/
      Read/
      List/
      Update/
      Delete/
    Services/
    TaskFeature.cs
```

Each operation keeps its handler, request, and response types together.

## Included Defaults

- Minimal API endpoint groups
- Request validation with `Microsoft.Extensions.Validation`
- Problem Details and a small global exception handler
- Basic fixed-window rate limiting
- `/alive` and `/health` checks
- Scalar/OpenAPI in development
- EF Core with PostgreSQL
- Redis distributed cache
- OpenTelemetry logs, traces, and metrics
- HTTP client service discovery and standard resilience handlers
- TUnit, Microsoft Testing Platform, Shouldly, and Aspire integration testing

## Template Package

```text
template/microservice-template.Template.csproj
template/.template.config/template.json
template/README.template.md
```

The root `README.md` is used as the NuGet package README. The generated project's README is sourced from `template/README.template.md`.

## Testing

```bash
dotnet build MicroserviceTemplate.slnx

dotnet test --solution MicroserviceTemplate.slnx

dotnet test --project tests/TemplateValidation.Tests/TemplateValidation.Tests.csproj
```

The integration tests require Docker because they start the Aspire AppHost with PostgreSQL and Redis.

The template validation test packs the template, installs it locally, generates a new service, builds that generated service, and runs its generated integration tests.

## Publishing

This repository includes CI for build, service tests, and template generation tests.

When you are ready to publish the template package, pack it and push the `.nupkg` to your preferred NuGet feed:

```bash
dotnet pack -c Release template/microservice-template.Template.csproj
```

## License

MIT
