# PRD: Dapper Interoperability

## Introduction

Add a lightweight, command-oriented interoperability layer that lets .NET teams use Redis OM's model mapping and RediSearch capabilities without adopting the `IQueryable` surface. This feature is intended for developers who prefer explicit query strings, parameter objects, and direct execution over LINQ translation, while still benefiting from Redis OM's schema, serialization, and materialization support.

The first version should focus on RediSearch read scenarios. It should provide a Dapper-like experience for query execution and result mapping while remaining a thin ergonomic layer over the existing `RedisQuery` capabilities rather than a separate low-level client.

## Goals

- Provide a Dapper-like alternative to `IQueryable` for RediSearch read operations.
- Reuse Redis OM's existing mapping and serialization behavior where practical.
- Support a broad set of query scenarios outside `IQueryable`, including scenarios currently handled through existing query primitives.
- Reduce the amount of raw string handling and manual deserialization required for command-oriented users.
- Keep the API explicit and unsurprising for developers who avoid LINQ-heavy data access patterns.

## User Stories

### US-001: Execute RediSearch queries without `IQueryable`
**Description:** As a .NET developer who prefers explicit commands, I want to execute RediSearch queries through a Dapper-like API so that I can avoid LINQ translation and still use Redis OM.

**Acceptance Criteria:**
- [ ] The public API allows executing a RediSearch query without constructing an `IQueryable`.
- [ ] The API accepts an index name or equivalent index-targeting mechanism.
- [ ] The API supports async execution for search queries.
- [ ] The API documentation includes at least one example showing query-string-based execution.
- [ ] Build passes for the affected projects.

### US-002: Bind named parameters for RediSearch queries
**Description:** As a .NET developer, I want to pass named parameters into query execution so that I can avoid manually concatenating values into RediSearch query strings.

**Acceptance Criteria:**
- [ ] The API accepts a parameter object, dictionary, or equivalent named-parameter input.
- [ ] Parameter binding supports standard RediSearch query parameter scenarios used by the library.
- [ ] Invalid or missing required parameters produce a deterministic, documented error path.
- [ ] At least one automated test verifies named parameter substitution behavior.
- [ ] Build passes for the affected projects.

### US-003: Materialize full documents into mapped models
**Description:** As a .NET developer, I want query results to materialize directly into mapped Redis OM models so that I can reuse existing document definitions.

**Acceptance Criteria:**
- [ ] Search query results can be materialized into registered Redis OM document types.
- [ ] Materialization reuses existing serialization and property mapping rules where applicable.
- [ ] Null or missing fields are handled consistently with existing Redis OM materialization behavior.
- [ ] At least one automated test verifies full-document materialization from the new API.
- [ ] Build passes for the affected projects.

### US-004: Materialize projections into typed DTOs
**Description:** As a .NET developer, I want to map query results into custom DTOs so that I can retrieve only the fields my application needs.

**Acceptance Criteria:**
- [ ] The API supports selecting a subset of returned fields for projection scenarios.
- [ ] Returned values can be materialized into a custom DTO type with a stable, documented mapping rule.
- [ ] A projection that omits unrelated document fields does not require full-document hydration.
- [ ] At least one automated test verifies typed DTO projection behavior.
- [ ] Build passes for the affected projects.

### US-005: Support anonymous-like projection ergonomics
**Description:** As a .NET developer, I want a lightweight projection experience for ad hoc reads so that simple field selection does not require a large amount of boilerplate.

**Acceptance Criteria:**
- [ ] The PRD defines a concrete V1 approach for anonymous-like projections, such as helper APIs, dynamic row objects, or another documented equivalent.
- [ ] The chosen projection approach is explicitly documented with examples and limitations.
- [ ] The API does not rely on unsupported C# anonymous type construction across public boundaries.
- [ ] At least one automated test covers the selected anonymous-like projection path.
- [ ] Build passes for the affected projects.

### US-006: Map aggregation results through the same interoperability layer
**Description:** As a .NET developer, I want aggregation results to use the same command-oriented experience so that I do not need a separate mental model for RediSearch aggregations.

**Acceptance Criteria:**
- [ ] The interoperability layer includes aggregation execution in scope for V1.
- [ ] Aggregation results can be materialized into a typed result model or documented row shape.
- [ ] The API clearly distinguishes search-result and aggregation-result behavior where needed.
- [ ] At least one automated test verifies aggregation result mapping.
- [ ] Build passes for the affected projects.

### US-007: Reuse existing `RedisQuery` internals without duplicating behavior
**Description:** As a maintainer, I want the new API to layer over existing query infrastructure so that behavior remains consistent and maintenance cost stays controlled.

**Acceptance Criteria:**
- [ ] The implementation uses `RedisQuery` or shared internal query/mapping infrastructure rather than reimplementing RediSearch execution from scratch.
- [ ] Public documentation describes the feature as a command-oriented layer, not a separate low-level Redis client.
- [ ] Tests demonstrate consistent behavior between the new API and existing query execution for equivalent read scenarios.
- [ ] Build passes for the affected projects.

### US-008: Add a runnable basic search example for the Dapper-style API
**Description:** As a developer evaluating the feature, I want a small runnable example that uses the Dapper-style API against a local Redis instance so that I can verify the setup and query flow end to end.

**Acceptance Criteria:**
- [ ] Add a new example project or extend an existing example to demonstrate basic Dapper-style RediSearch query execution.
- [ ] The example includes local Redis connection setup assumptions or instructions.
- [ ] The example can be run against a local Redis instance without requiring remote services.
- [ ] The example seeds or assumes enough local data to demonstrate a successful query result.
- [ ] The example includes an automated or scripted validation step that confirms the example works against a local Redis instance.
- [ ] Build passes for the affected projects.

### US-009: Add a runnable parameterized projection example
**Description:** As a developer, I want an example showing named parameters and projection mapping so that I can understand the most important Dapper-style read pattern quickly.

**Acceptance Criteria:**
- [ ] Add an example demonstrating named query parameters with a projected result shape.
- [ ] The example targets a local Redis instance and does not depend on cloud-hosted infrastructure.
- [ ] The example output or validation clearly shows that parameter binding and projection mapping succeeded.
- [ ] The example includes instructions or automation for local execution.
- [ ] The example includes an automated or scripted validation step that confirms the example works against a local Redis instance.
- [ ] Build passes for the affected projects.

### US-010: Add a runnable aggregation example
**Description:** As a developer, I want an example for aggregation queries through the Dapper-style API so that I can validate the non-document result path before adopting it.

**Acceptance Criteria:**
- [ ] Add an example demonstrating aggregation execution through the new command-oriented API.
- [ ] The example targets a local Redis instance and can run with local sample data.
- [ ] The example output or validation clearly shows that aggregation result mapping succeeded.
- [ ] The example includes instructions or automation for local execution.
- [ ] The example includes an automated or scripted validation step that confirms the example works against a local Redis instance.
- [ ] Build passes for the affected projects.

### US-011: Provide local example validation workflow
**Description:** As a maintainer, I want a repeatable local validation workflow for the new examples so that example regressions are caught early and the examples remain trustworthy.

**Acceptance Criteria:**
- [ ] Add a documented workflow or script for running the Dapper-style examples against a local Redis instance.
- [ ] The workflow specifies required local prerequisites, including the expected Redis modules or configuration.
- [ ] The workflow produces a failing exit code when an example does not execute successfully.
- [ ] The workflow is suitable for local development use without requiring GitHub-hosted services.
- [ ] Build passes for the affected projects.

## Functional Requirements

1. FR-1: The system must provide a public, command-oriented API for executing RediSearch reads without requiring `IQueryable`.
2. FR-2: The API must be a thin ergonomic layer on top of `RedisQuery` or the same underlying internal query and mapping infrastructure.
3. FR-3: The API must support asynchronous search query execution.
4. FR-4: The API must allow callers to specify the target index for a query.
5. FR-5: The API must accept named parameters for RediSearch query execution instead of requiring callers to manually interpolate all values into query strings.
6. FR-6: The API must support broad read/query scenarios in the first version, including search, projection, aggregation, and other equivalent non-write query cases already aligned with Redis OM's RediSearch scope.
7. FR-7: The API must materialize full search results into mapped Redis OM document types.
8. FR-8: The API must support mapping returned fields into typed DTO projections.
9. FR-9: The API must define and document a supported V1 approach for anonymous-like projections.
10. FR-10: The API must support aggregation execution and aggregation result mapping through the same command-oriented surface.
11. FR-11: The API must preserve existing Redis OM serialization and field-mapping conventions wherever reuse is practical.
12. FR-12: The system must provide deterministic error behavior for invalid query input, invalid parameters, and unsupported projection/materialization requests.
13. FR-13: The implementation must include automated test coverage for search execution, parameter binding, full-document materialization, DTO projection, anonymous-like projection behavior, and aggregation mapping.
14. FR-14: The documentation must include examples for search queries, named parameters, full-document mapping, DTO projection, anonymous-like projection usage, and aggregation scenarios.
15. FR-15: The feature must include runnable examples for the Dapper-style API that target a local Redis instance.
16. FR-16: At least one runnable example must demonstrate basic search execution, one must demonstrate parameterized projection, and one must demonstrate aggregation.
17. FR-17: Example projects or scripts must include a repeatable validation path that confirms they work against a local Redis instance and exits non-zero on failure.
18. FR-18: Example documentation must state the required local Redis prerequisites, including any required RediSearch-capable local setup.

## Non-Goals

- Supporting writes, updates, deletes, or transactional workflows through this interoperability layer.
- Becoming a general-purpose low-level Redis client abstraction.
- Recreating all Dapper features unrelated to Redis OM's RediSearch and mapping responsibilities.
- Achieving full LINQ parity or replacing the `IQueryable` surface in V1.
- Redesigning Redis OM's core model attribute system or schema-definition approach.

## Design Considerations

- The API should feel explicit and command-oriented, with simple method names and minimal hidden behavior.
- The shape should be approachable for developers familiar with Dapper patterns such as query execution plus typed materialization.
- Examples should emphasize readability and predictable control flow over fluent complexity.
- Projection ergonomics need particular care because anonymous C# types are not a straightforward public contract; the design should make the compromise explicit.

## Technical Considerations

- Reuse existing `RedisQuery` internals where possible to avoid divergent RediSearch behavior.
- Confirm how current Redis OM materialization handles missing fields, nullable values, aliases, and partial result sets so the new API remains consistent.
- Aggregation mapping may need a result-shaping abstraction distinct from full-document hydration.
- Parameter binding should cover both standard RediSearch parameters and vector-related parameters if those are already within the library's read-query scope.
- The implementation should avoid introducing a second competing query engine when a wrapper over current internals can satisfy the requirement.
- The example workflow should prefer existing repository conventions for local Redis usage so examples are easy to run for maintainers and contributors.
- Example validation should be runnable locally and should not depend on GitHub Actions or any remote Redis environment.
- Any GitHub-based implementation workflow, issue reference, pull request, or automation interaction for this work must target the fork at `https://github.com/slorello89/redis-om-dotnet` rather than the upstream repository.

## Success Metrics

- A developer can execute a RediSearch read without touching `IQueryable` or manual deserialization.
- The feature supports the common read/query scenarios a command-oriented team expects from Redis OM's RediSearch scope.
- Example code for the new API is shorter and clearer than the equivalent raw-string plus manual-mapping approach.
- Maintainers can implement the feature primarily by reusing existing query infrastructure rather than duplicating it.
- The new API can be documented as an adoption-friendly entry point for teams skeptical of LINQ-heavy query translation.
- Contributors can run the shipped Dapper-style examples against a local Redis instance and get deterministic validation results.

## Open Questions

- What exact public type should represent anonymous-like projection results in V1?
- Which current `RedisQuery` capabilities can be exposed directly without leaking too much internal complexity?
- Should the API be attached to the existing connection object, a new service object, or extension methods?
- How much vector-query parameter support should be guaranteed in V1 versus documented as incremental follow-up work?
- Are there existing RediSearch read scenarios in Redis OM that cannot cleanly fit a Dapper-like command model and therefore need explicit exclusions?
- Should the examples live as new standalone projects, extensions of existing examples, or a mix of both?
