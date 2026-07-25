# ms-chat and ms-scheduler template delta review

Date: 2026-07-02

Scope:

- Template baseline: `C:\Users\halvo\Code\microservice-template`
- Compared apps: `C:\Users\halvo\Cillco\ms-chat` and `C:\Users\halvo\Cillco\ms-scheduler`
- Angular surfaces were not reviewed for product/UI quality. I only looked at frontend-related AppHost wiring where it affects backend/dev ergonomics.

## Baseline already in the template

The template is not bare bones. It already has several niceties that also appear in the two apps:

- Feature-sliced Minimal API structure with operation handlers and per-feature registration.
- Central package management, analyzers, nullable, latest language version, and CI warning policy.
- Aspire AppHost with PostgreSQL and Redis resources.
- EF Core with migrations and retry-on-failure for PostgreSQL.
- Redis distributed cache sample in the task feature.
- OpenTelemetry logs, traces, metrics, service resource attributes, OTLP toggle, and sampling ratio.
- `/health` readiness and `/alive` liveness endpoints.
- ProblemDetails plus a global exception handler with observability.
- Fixed-window API rate limiting with `Retry-After` and ProblemDetails rejections.
- Scalar/OpenAPI in development.
- TUnit integration tests that boot the Aspire AppHost and wait for resources/database readiness.
- Template validation tests that pack/install/generate/build/test a generated service.
- GitHub Actions for CI and manual template package release.

That means the deltas below are mostly product hardening, local-dev ergonomics, API contract polish, and reusable optional patterns.

## Pass 1: Solution and local topology niceties

### 1. Optional frontend/resource wiring in AppHost

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat.AppHost\AppHost.cs`
- `C:\Users\halvo\Cillco\ms-chat\tests\MsChat.IntegrationTests\Common\TestFixture.cs`

What is nice:

- `ms-chat` wires the web app only unless `AppHost:SkipWeb=true`.
- Integration tests pass `AppHost:SkipWeb=true`, so backend tests avoid paying for the Angular app.

Template gap:

- The template has a single API resource and no example for optional non-API resources.

Adoption idea:

- Add a documented AppHost pattern for optional resources: `AppHost:SkipWeb`, `AppHost:SkipWorkers`, or similar.
- Keep it as a recipe or commented pattern, not default runtime behavior.

### 2. Representative local blob/object storage

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat.AppHost\AppHost.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Configurations\Options\AttachmentStorageOptions.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Attachments\Services\AttachmentStorageService.cs`

What is nice:

- `ms-chat` starts Floci as local S3-compatible storage and injects attachment storage options into the API.
- The app validates bucket/region config on startup.
- Attachments are modeled as metadata in PostgreSQL with bytes outside the relational database.

Template gap:

- The template models PostgreSQL and Redis, but no object storage option or "do not store bytes in Postgres" pattern.

Adoption idea:

- Add an optional "blob storage feature recipe" to the template docs or a template switch later.
- Include validated options and an abstraction boundary, but avoid making blob storage part of the default minimal generated service.

### 3. AppHost command for seeding realistic local data

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.AppHost\AppHost.cs`

What is nice:

- Scheduler adds a highlighted Aspire command, `seed-schedules`, that calls the public API and creates realistic local data.
- It reports created IDs and can trigger immediate executions.

Template gap:

- The template has seeded data through EF/development setup, but no AppHost command example.

Adoption idea:

- Add a simple highlighted AppHost command such as `seed-tasks` or document the pattern.
- This is a good template nicety because it teaches "seed through the public API" rather than quietly mutating the database.

### 4. Multi-instance local topology

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.AppHost\AppHost.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.IntegrationTests\Fixtures\SchedulerAppFixture.cs`

What is nice:

- Scheduler starts two API instances against the same database and dev proxy.
- Integration tests wait for both resources and verify a due one-time schedule executes once with two instances.

Template gap:

- The template only models a single service instance.

Adoption idea:

- Add a documented "run a second instance" recipe for services with background workers, leases, or distributed locks.
- Probably not a default because many generated services do not need multi-instance tests on day one.

### 5. Downstream service simulation with Dev Proxy

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.AppHost\AppHost.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.IntegrationTests\Fixtures\SchedulerAppFixture.cs`

What is nice:

- Scheduler uses Microsoft Dev Proxy as a local downstream simulator.
- Tests cover success and failure delivery paths without depending on real external services.

Template gap:

- The template has resilient HTTP defaults but no downstream simulator/testing pattern.

Adoption idea:

- Add a recipe for "testing outbound HTTP" with either Dev Proxy or a lightweight local mock server.
- This belongs in docs or an optional sample because not every microservice calls out.

## Pass 2: API, contracts, and domain hardening

### 6. Tenant/user request context abstraction

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Services\Internal\ChatContext.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Common\TenantContext.cs`

What is nice:

- `ms-chat` resolves tenant/user from claims first, then headers, and for SignalR can include query string values.
- It has an ambient `AsyncLocal` scope so hub calls and HTTP handlers can use the same request-context accessor.
- `ms-scheduler` has a simpler tenant context with trimming and max length enforcement.

Template gap:

- The template has no first-class current tenant/user context.

Adoption idea:

- Add an optional `RequestContext` pattern to template docs, or a small default abstraction if multi-tenant services are common.
- Make security boundaries explicit: headers are only safe behind trusted auth/proxy infrastructure or in local/dev tests.

### 7. Tenant-aware rate-limit partitioning

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Configurations\Setup\RateLimitingSetup.cs`

What is nice:

- Scheduler partitions API limits by `X-Tenant-Id` when present, falling back to remote IP.
- Invalid overlong tenant IDs get their own partition rather than causing rate-limit key explosion.

Template gap:

- The template partitions by authenticated user name or IP only.

Adoption idea:

- Add a reusable rate-limit partition hook, or document how tenant-aware services should replace the default partition key.

### 8. Domain problem exception mapped by the global handler

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Common\GlobalExceptionHandler.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Services\Internal\ChatProblemException.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Services\Internal\ChatProblems.cs`

What is nice:

- Chat handlers throw a domain exception with an HTTP status.
- The global handler turns that into consistent ProblemDetails with a feature-specific error type.

Template gap:

- The template has global exception handling but no generic feature/domain problem exception pattern.

Adoption idea:

- Add a small `ProblemException` or `ApplicationProblemException` base class to the template.
- Use it sparingly for expected domain failures, while keeping direct typed results for simple endpoints.

### 9. Idempotency keys backed by unique constraints

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Messages\Operations\Send\SendMessageHandler.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Services\Internal\DatabaseConstraints.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Operations\Create\CreateScheduleHandler.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.IntegrationTests\Tests\ScheduleApiTests.cs`
- `C:\Users\halvo\Cillco\ms-chat\tests\MsChat.IntegrationTests\Tests\MessageTests.cs`

What is nice:

- Duplicate creates/sends can return the existing durable resource.
- Both services test concurrent duplicate submissions.

Template gap:

- The template CRUD sample has no idempotency key pattern.

Adoption idea:

- Add an "idempotent create" example to the generated task feature or docs.
- This is one of the highest-value reusable patterns for real microservices.

### 10. ETags and `If-Match` for optimistic concurrency

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\ScheduleEndpoints.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\ScheduleEtags.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.IntegrationTests\Tests\ScheduleApiTests.cs`

What is nice:

- Scheduler returns an `ETag` based on a version value.
- Update/patch/delete can reject stale clients with HTTP 412.

Template gap:

- The template update/delete sample has no conditional request support.

Adoption idea:

- Add an optional optimistic concurrency recipe.
- Avoid putting it in the default task CRUD unless the template wants to teach production-grade write semantics over simplicity.

### 11. More complete endpoint metadata

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\ScheduleEndpoints.cs`

What is nice:

- Scheduler endpoints use `Accepts`, `Produces`, `ProducesProblem`, named routes, summaries, common problem metadata, and versioned plus legacy route groups.

Template gap:

- The template endpoints have names/summaries but little response metadata.

Adoption idea:

- Add a compact `.ProducesCommonProblems()` extension and response metadata to the template task endpoints.
- This improves OpenAPI quality without changing architecture.

### 12. Pagination and limit clamping helpers

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\ScheduleEndpoints.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Messages\Operations\List\ListMessageHandler.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Operations\ListEvents\ListChatEventsHandler.cs`

What is nice:

- Query limits are bounded to keep endpoints predictable.
- Scheduler has a reusable `PagedResult<T>`.

Template gap:

- Template `ListTasks` returns all tasks and has no paging pattern.

Adoption idea:

- Add paging to the sample list endpoint, even if simple.
- This is a strong candidate for the default template because unbounded list endpoints are rarely what you want.

### 13. Soft delete/archive pattern

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Models\ArchivedSchedule.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.IntegrationTests\Tests\ScheduleApiTests.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Models\Status.cs`

What is nice:

- Scheduler archives schedules before removing active records.
- Chat archives conversations and blocks invalid reactivation.

Template gap:

- Template task delete is a hard delete.

Adoption idea:

- Add a documented archive/soft-delete variant.
- Keep hard delete in the minimal starter unless the template wants to bias toward audit-friendly service behavior.

### 14. External target hardening

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\Execution\HttpTargetPolicy.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\Execution\HttpJobExecutor.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.UnitTests\ScheduleFeatureServiceTests.cs`

What is nice:

- Outbound targets are validated against allowed schemes and optional host allow-lists.
- Reserved scheduler identity headers are protected from user-supplied overrides.

Template gap:

- The template has resilient outbound HTTP defaults, but no outbound security policy pattern.

Adoption idea:

- Add a docs recipe for outbound calls: allowed hosts, reserved headers, timeout/retry policy, and tests.

### 15. Service command registry

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\Execution\ServiceCommandRegistry.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Configurations\Options\SchedulerOptions.cs`

What is nice:

- Scheduler stores stable service/command names and resolves concrete URLs/methods from configuration.
- This decouples schedule records from environment-specific downstream URLs.

Template gap:

- No pattern for stable logical outbound commands.

Adoption idea:

- Keep this as a pattern note for workflow/orchestration services, not a default template feature.

### 16. Input normalization helpers beside data annotations

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Services\Internal\ChatInput.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\ScheduleInputValidator.cs`

What is nice:

- Both services validate constraints that are awkward for attributes: content types, file names, target URLs, enum definitions, wildcard-safe search, cron expressions, and persisted field lengths.

Template gap:

- The template uses request validation but does not show where richer cross-field or domain validation belongs.

Adoption idea:

- Add a small operation-level validator example for create/update beyond attributes.
- Especially useful if the template keeps feature-sliced handlers.

## Pass 3: Data, workers, observability, and testing

### 17. Startup database initializer with PostgreSQL advisory lock

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Infrastructure\Data\DatabaseInitializer.cs`

What is nice:

- Scheduler can initialize its DB on startup using migrations or `EnsureCreated`.
- It takes a PostgreSQL advisory lock so multiple instances do not migrate concurrently.
- It logs skip/start/complete/failure events.

Template gap:

- Template only migrates in development and does not model multi-instance migration safety.

Adoption idea:

- Consider adding a production-aware initializer recipe.
- Be careful making migrations-on-start the default; many teams want migrations owned by deployment pipelines.

### 18. Background execution shape

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\Execution\ManualExecutionQueue.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\Execution\ManualExecutionWorker.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\Execution\ScheduleExecutionRunner.cs`

What is nice:

- Scheduler separates HTTP API requests, background dispatch, execution runner, executor selection, and audit rows.
- Manual execution returns 202 Accepted with a location for the execution resource.

Template gap:

- The template has only synchronous CRUD request/response behavior.

Adoption idea:

- Add a "background job/worker" optional recipe if the template is meant to seed microservices that do async work.

### 19. Richer observability dimensions for product operations

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Common\MsChatTelemetry.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Messages\MessageObservability.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Common\SchedulerTelemetry.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\Features\Schedules\Services\Execution\ScheduleExecutionObservability.cs`

What is nice:

- Both apps add feature-specific tags and metrics, while scheduler explicitly avoids high-cardinality identifiers in metrics and keeps IDs mainly in traces/logs.
- Scheduler distinguishes API schedule operations from background execution attempts.

Template gap:

- The template has a good telemetry skeleton, but the task sample is simpler and does not demonstrate background/async operation telemetry or cardinality guidance.

Adoption idea:

- Add a short telemetry guideline to the generated README: what goes in logs, metrics, and traces; avoid tenant/resource IDs in metric labels.

### 20. SignalR realtime pattern with Redis backplane and tests

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Hubs\ChatHub.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\ChatFeature.cs`
- `C:\Users\halvo\Cillco\ms-chat\tests\MsChat.IntegrationTests\Tests\RealtimeHubTests.cs`

What is nice:

- Typed hub client interface.
- Authorization check before group join.
- Redis backplane when cache connection exists.
- Integration tests for join rejection, leave behavior, typing behavior, and post-commit message broadcast.

Template gap:

- No realtime example.

Adoption idea:

- Do not add SignalR to the default template.
- Add a separate recipe or template option for realtime services.

### 21. Durable event stream for reconnect/catch-up

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Models\Event.cs`
- `C:\Users\halvo\Cillco\ms-chat\src\MsChat\Features\Chats\Services\Internal\ChatEventWriter.cs`
- `C:\Users\halvo\Cillco\ms-chat\tests\MsChat.IntegrationTests\Tests\ReadStateAndEventTests.cs`

What is nice:

- Chat does not rely on realtime delivery being perfect.
- Clients can replay durable events after a cursor.

Template gap:

- No event/audit/replay pattern.

Adoption idea:

- Consider an "audit/event log" recipe, especially paired with realtime or external notifications.

### 22. Integration-test fixtures as domain clients

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\tests\MsChat.IntegrationTests\Common\TestFixture.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.IntegrationTests\Fixtures\SchedulerAppFixture.cs`
- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.IntegrationTests\Fixtures\ApiAssertions.cs`

What is nice:

- Fixtures expose domain-specific helpers instead of each test hand-writing raw HTTP.
- Chat fixture creates authorized HTTP requests and SignalR hub connections with tenant/user context.
- Scheduler fixture creates tenant-specific clients, schedule request builders, and execution polling helpers.
- `ApiAssertions` includes "assert status with response body" helpers for better failure messages.

Template gap:

- Template fixture has useful HTTP helpers, but the tests still do more raw HTTP and inline assertions.

Adoption idea:

- Add a small `ApiAssertions` file and move sample request builders into the fixture.
- This is low risk and improves generated test ergonomics.

### 23. Unit tests for domain services and serialization

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\tests\Cillco.Service.Scheduler.UnitTests`

What is nice:

- Scheduler tests JSON polymorphism, execution policy serialization, validator behavior, tenant context, target policy, and ticker factory logic without booting Aspire.

Template gap:

- The template has integration tests and template validation tests, but no unit-test project.

Adoption idea:

- Add an optional unit-test project when generated services include non-trivial domain services.
- Maybe do not include by default unless the starter task feature grows enough to justify it.

### 24. Architecture and operations docs

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\README.md`
- `C:\Users\halvo\Cillco\ms-scheduler\ARCHITECTURE.md`
- `C:\Users\halvo\Cillco\ms-scheduler\OPERATIONS.md`

What is nice:

- `ms-chat` explains service boundaries, local topology, primitives, and data flows.
- Scheduler splits architecture decisions from operations/runbook details.

Template gap:

- Template README is good for generated-service usage, but generated services do not get separate architecture/operations doc stubs.

Adoption idea:

- Add optional `ARCHITECTURE.md` and `OPERATIONS.md` templates or a section in `README.template.md`.
- A generated service should have placeholders for boundary, persistence, health, telemetry, local Aspire, migrations, and common troubleshooting.

## Items I would not copy as-is

### Angular implementations

Not reviewed per request. I only noted AppHost patterns that affect backend local development.

### Scheduler environment config leftovers

Seen in:

- `C:\Users\halvo\Cillco\ms-scheduler\src\Cillco.Service.Scheduler.Api\appsettings.Development.json`

Why not copy:

- It contains Azure Key Vault/App Configuration, Consul, Keycloak, and JWT sections, but the inspected backend startup does not wire those systems.
- These may be useful in the real environment, but they are not proven reusable template niceties from this pass.

### Potential docs drift

Seen in:

- `C:\Users\halvo\Cillco\ms-chat\README.md`
- `C:\Users\halvo\Cillco\ms-scheduler\SCHEDULER_CLIENT_USAGE.md`

Why not copy:

- `ms-chat` README mentions `MsChat.ServiceDefaults`, but the current file list did not show that project.
- Scheduler client docs describe a client library, but the backend file scan did not show an active client project in the solution shape I reviewed.
- These docs still contain good examples of explanatory style, but I would verify them before porting content.

## Suggested adoption order

High value, broadly applicable:

1. Idempotent create pattern with unique constraints and concurrent tests.
2. Paged list endpoints with bounded page size.
3. Better endpoint OpenAPI metadata plus common ProblemDetails metadata.
4. Domain/application `ProblemException` pattern.
5. Test fixture/domain-client helpers and `ApiAssertions`.
6. Architecture/operations doc stubs for generated services.

Medium value, useful for common production services:

1. Tenant/user request context abstraction.
2. Tenant-aware rate-limit partition hook.
3. Optimistic concurrency with ETags/`If-Match`.
4. AppHost seed command through the public API.
5. Startup DB initializer recipe with advisory lock.
6. Telemetry cardinality guidance and background-operation observability example.

Optional recipes/template switches:

1. Object/blob storage with S3-compatible local dependency.
2. SignalR realtime with Redis backplane.
3. Durable event stream for replay/catch-up.
4. Multi-instance AppHost topology.
5. Dev Proxy/downstream HTTP simulator.
6. Background worker/manual execution queue pattern.
7. Service-command registry and outbound target policy.
