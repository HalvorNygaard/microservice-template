# Operations

## Local development

```bash
dotnet tool restore
aspire start --non-interactive
```

The Aspire dashboard exposes the API, PostgreSQL, health state, logs, traces, and the `seed-tasks` command. The command creates sample tasks through the public HTTP API; migrations contain no sample data.

## Database migrations

Create a migration after changing the EF model:

```bash
dotnet ef migrations add ChangeName --project src/MicroserviceTemplate/MicroserviceTemplate.csproj --startup-project src/MicroserviceTemplate/MicroserviceTemplate.csproj
```

CI verifies that the checked-in migrations match the current model. Production migration execution should be a separate deployment step with an appropriately privileged identity; the application only auto-migrates in Development.

## Containers and cloud deployment

The API can be published as a framework-dependent artifact or SDK-built container:

```bash
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release /t:PublishContainer
```

Supply the `postgresdb` connection string using the platform's secret/configuration system. Set `OTEL_EXPORTER_OTLP_ENDPOINT` to export telemetry. Terminate TLS at the ingress or platform proxy and configure forwarded headers according to that platform.

Before production, decide and document authentication, authorization, ingress or distributed rate limiting, secret rotation, migration ownership, backup/restore, scaling limits, resource requests, disruption behavior, and alerting objectives. Those choices cannot be safely genericized in a minimal template.
