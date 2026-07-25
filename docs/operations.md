# Operations

## Local development

```bash
dotnet tool restore
aspire start --non-interactive
```

Use the Aspire dashboard to open the API and inspect PostgreSQL, health, logs, traces, and metrics.

## Database migrations

Create a migration after changing the EF model:

```bash
dotnet ef migrations add ChangeName --project src/MicroserviceTemplate/MicroserviceTemplate.csproj --startup-project src/MicroserviceTemplate/MicroserviceTemplate.csproj
```

Before release, run `dotnet ef migrations has-pending-model-changes` to confirm the checked-in migrations match the model. Production migration execution should be a separate deployment step with an appropriately privileged identity; the application only auto-migrates in Development.

## Containers and deployment

The API can be published as a framework-dependent artifact or an SDK-built container:

```bash
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release /t:PublishContainer
```

The project sets the container repository, image format, and HTTP port `8080`. The .NET SDK chooses its supported base image and non-root runtime user. Deployment automation should supply the registry and image tags.

Supply the `postgresdb` connection string through the deployment platform's secret/configuration system.

## OpenTelemetry deployment reference

The service owns its instrumentation in code. Aspire supplies the service name, instance identity, and local OTLP destination while orchestrating the AppHost. In other environments, configure that deployment-owned identity and export behavior with standard OpenTelemetry variables:

```text
OTEL_SERVICE_NAME=apiservice
OTEL_RESOURCE_ATTRIBUTES=service.version=<release-version>,deployment.environment.name=<environment>
OTEL_EXPORTER_OTLP_ENDPOINT=https://<collector>
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_TRACES_SAMPLER=parentbased_traceidratio
OTEL_TRACES_SAMPLER_ARG=0.10
```

Set exporter headers, compression, and timeout only when required by the selected collector. Do not check in a fixed `service.instance.id`; Aspire or the production orchestrator should assign an identity per running instance. Add code-owned resource attributes in `ConfigureOpenTelemetry` only when they are invariant properties of the service rather than deployment metadata.

The default request budgets are 30 seconds for API operations and 5 seconds for health checks. Override `RequestTimeouts:ApiTimeout` or `RequestTimeouts:HealthCheckTimeout` only with an operational reason; invalid or unbounded values fail startup.

Before production, decide and document authentication, authorization, ingress rate limiting, secret rotation, migration ownership, backup/restore, scaling limits, resource requests, disruption behavior, and alerting objectives.

Use `docs/greenfield.md` as temporary service-inception scaffolding. Delete it after its applicable decisions have moved into durable service documentation and the first real slice passes the service's delivery gates.
