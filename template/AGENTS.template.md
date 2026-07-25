# Agent guide

This repository is an owned .NET microservice created from the Modern Microservice template. Keep it small, portable, and easy for another agent to navigate.

## Working rules

- Prefer vertical slices under `Features/<Feature>/<Operation>`; colocate each operation's route, contract, and behavior.
- Keep `Program.cs` as composition only. Put cross-cutting setup in focused extension methods.
- Use platform features before adding dependencies. Every package and infrastructure resource must earn its place.
- Keep domain invariants on domain types. Use `TimeProvider` for time and UUID v7 for chronological entity identifiers.
- Return Problem Details for API errors and preserve request and trace correlation.
- Assign the API request-timeout policy to operations and the health-check policy to health endpoints.
- Keep telemetry labels low-cardinality and use standard OpenTelemetry configuration.
- Treat writes as non-retryable unless idempotency is explicitly designed.
- Keep database migrations deterministic and free of sample data.
- Add pagination only when a collection contract requires it. Prefer opaque keyset cursors for large, changing collections.
- Add optimistic concurrency only when concurrent writers are a real concern. For PostgreSQL, prefer `xmin` for an internal EF token or an opaque random token exposed as a strong ETag; do not use UUID v7 as a concurrency token.
- Do not add authentication implicitly. This service has no authentication or authorization until its owner selects and documents an identity model.

The raw service identity is `MicroserviceTemplate`; its C# root namespace is `ModernMicroservice`. Preserve that distinction when adding projects, namespaces, infrastructure resources, or deployment identities.

## Verification

Run these before handing work off:

```bash
dotnet tool restore
dotnet build MicroserviceTemplate.slnx -c Release
dotnet test --project tests/MicroserviceTemplate.UnitTests/MicroserviceTemplate.UnitTests.csproj -c Release --no-build
dotnet test --project tests/MicroserviceTemplate.IntegrationTests/MicroserviceTemplate.IntegrationTests.csproj -c Release --no-build
dotnet ef migrations has-pending-model-changes --project src/MicroserviceTemplate/MicroserviceTemplate.csproj --startup-project src/MicroserviceTemplate/MicroserviceTemplate.csproj --no-build --configuration Release
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release --no-build
```

Integration tests require Docker.

Work through `docs/greenfield.md` while turning the reference service into the owned service. Remove it only after the applicable decisions are recorded durably and the first real vertical slice passes this verification contract.
