# Modern Microservice Template

A minimal .NET 10 template for creating modern, Aspire-backed microservices with a vertical-slice feature layout.

The template generates a small service with:

- one API project
- one Aspire AppHost project
- PostgreSQL and Redis resources
- EF Core with migrations
- OpenTelemetry, health checks, and resilient HTTP defaults
- Scalar/OpenAPI in development
- integration tests
- central package management, analyzers, and `.editorconfig`

## Local template flow

This repo is not published to NuGet.org yet. To use it locally, pack and install the template package from the repo:

```bash
dotnet pack -c Release template/microservice-template.Template.csproj

dotnet new install ./template/bin/Release/ModernMicroservice.Template.*.nupkg

dotnet new modern-microservice -n MyService

cd MyService

dotnet run --project src/MyService.AppHost/MyService.AppHost.csproj
```

To remove the locally installed template later:

```bash
dotnet new uninstall ModernMicroservice.Template
```

## Generated project

A generated service uses `<ServiceName>` as the root namespace and project name.

The service is intentionally small:

```text
src/
  <ServiceName>/
  <ServiceName>.AppHost/
tests/
  <ServiceName>.IntegrationTests/
```

Application behavior lives in vertical feature folders under `Features/`. Cross-cutting service defaults live in `Common/`, `Configurations/`, `Infrastructure/Data/`, and `Program.cs`.

## Template internals

The template package is defined in:

```text
template/microservice-template.Template.csproj
template/.template.config/template.json
```

The generated-project README is sourced from `template/README.template.md`.

The template-only vision document is intentionally excluded from generated projects.

## Testing the template repo

Run the service and integration tests:

```bash
dotnet test MicroserviceTemplate.slnx
```

Run the template generation validation:

```bash
dotnet test tests/TemplateValidation.Tests/TemplateValidation.Tests.csproj
```

Integration tests require Docker because they start the Aspire AppHost with PostgreSQL and Redis.

## Publishing later

When ready, publish the packed `.nupkg` to a NuGet feed. The current GitHub workflow publishes to GitHub Packages on pushes to `main`.

For the lowest-friction public setup, NuGet.org is simpler because GitHub Packages requires authentication even for public packages.

## License

MIT
