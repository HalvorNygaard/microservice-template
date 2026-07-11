# Agent guide

This repository is a reusable .NET microservice template. Keep generated services small, cloud-portable, and easy for another agent to navigate.

## Working rules

- Prefer vertical slices under `Features/<Feature>/<Operation>`; colocate each operation's route, contract, and behavior.
- Keep `Program.cs` as composition only. Put cross-cutting setup in focused extension methods.
- Use platform features before adding dependencies. Every package and infrastructure resource must earn its place.
- Keep domain invariants on the domain type. Use `TimeProvider` for time and UUID v7 for new identifiers.
- Return RFC 9457 Problem Details for API errors and preserve trace correlation.
- Treat writes as non-retryable unless idempotency is explicitly designed.
- Keep database migrations deterministic and free of sample data. Seed local examples through AppHost commands.
- Do not add authentication implicitly. A generated service has no authentication or authorization until its owner selects an identity model.

## Verification

Run these before handing work off:

```bash
dotnet tool restore
dotnet build MicroserviceTemplate.slnx -c Release
dotnet test --solution MicroserviceTemplate.slnx -c Release --no-build
dotnet ef migrations has-pending-model-changes --project src/MicroserviceTemplate/MicroserviceTemplate.csproj --startup-project src/MicroserviceTemplate/MicroserviceTemplate.csproj --no-build
dotnet publish src/MicroserviceTemplate/MicroserviceTemplate.csproj -c Release --no-build
```

Integration and template-validation tests require Docker.
