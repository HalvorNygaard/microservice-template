# Greenfield service checklist

This document is temporary scaffolding for turning the generated service into an owned domain service. Work through it while delivering the first production-shaped vertical slice.

Delete this file only after the chosen answers are recorded in the service's durable README, architecture notes, operations runbook, or deployment configuration, and the first vertical slice is green through CI, migration, and container smoke tests.

## Establish the domain

- Name the service capability and the business outcome it owns.
- Identify the first tracer-bullet operation, its caller, contract, invariants, and observable success or failure.
- Replace the Tasks example instead of building a permanent generic application layer around it.
- Decide which data is authoritative here and which data is referenced from another owner.

## Choose infrastructure from requirements

- PostgreSQL and EF Core are included. Remove them if the service does not own relational state.
- Add messaging only when asynchronous delivery, buffering, fan-out, or durable integration is required. Document delivery guarantees, idempotency, poison handling, and ownership.
- Add object storage only for payloads that do not belong in the database. Define metadata ownership, cleanup, reconciliation, and health behavior.
- Add a worker only for durable background responsibility. Define lease/concurrency behavior, bounded retry, failure visibility, shutdown, and readiness.
- Keep rate limiting at ingress unless the service needs a tested distributed policy.

## Define the HTTP contract

- Use operation-local request and response contracts.
- Add pagination only when a collection needs it. Prefer opaque keyset cursors for large, changing collections.
- Add optimistic concurrency only when concurrent writers are a real concern. With PostgreSQL, prefer `xmin` for an internal EF token or an opaque random token exposed as a strong ETag. Keep UUID v7 for chronological entity identifiers, not concurrency.
- Assign the API timeout policy to operations and the health-check policy to health endpoints.
- Keep reusable HTTP failures under `/problems/common/`, domain failures under the generated service namespace,
  and expose one stable machine-readable `code`. If your organization owns a durable problem-documentation host,
  replace the relative root with that controlled absolute URI before publishing the API contract.
- Define idempotency before allowing automatic retries of writes.

## Make identity explicit

The template intentionally selects no authentication system. Before exposing protected behavior, document:

- issuer, audience, credential flow, and token validation owner;
- caller kinds such as user, workload, or delegated workload;
- authorization policies and the resource or tenant boundary they enforce;
- local-development credentials and production secret rotation;
- which endpoints, if any, are intentionally anonymous.

Do not infer these decisions from another generated service.

## Complete production ownership

- Define low-cardinality business metrics and trace boundaries for the first slice.
- Start from the editable OpenTelemetry deployment reference in `docs/operations.md`; configure OTLP destinations, sampling, release metadata, dashboards, and actionable alerts.
- Confirm `/alive` only represents process viability and `/health` represents traffic readiness.
- Decide who executes migrations, backs up and restores data, handles incidents, and owns dependencies.
- Set resource limits, scaling behavior, disruption expectations, and deployment rollback criteria.
- Add realistic journey and failure-path tests.
- Verify migration/model parity and smoke the production container before release.

## Removal gate

Before deleting this checklist, confirm:

1. The Tasks example is removed or deliberately retained as real domain code.
2. Durable documentation contains every applicable decision above.
3. Authentication remains explicitly absent or has a documented, tested owner and model.
4. The first slice passes formatting, warning-free Release build, unit/integration tests, EF drift, migration idempotence, publish, and container health gates.
