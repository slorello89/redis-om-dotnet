# Redis.OM Dapper Interoperability Example

This example demonstrates the provider-level RediSearch APIs added for command-oriented usage:

- basic search execution with `SearchAsync<T>`
- parameterized projections with DTO and `SearchProjection` results
- aggregation execution with `AggregateAsync<T>`

## Prerequisites

- .NET SDK installed
- A local Redis Stack or other RediSearch-capable Redis instance
- Redis reachable at `redis://localhost:6379`, or set `REDIS_OM_EXAMPLE_REDIS_URL`

## Run

```bash
dotnet run --project examples/Redis.OM.DapperInteroperability/Redis.OM.DapperInteroperability.csproj
```

## Validate Locally

```bash
bash examples/Redis.OM.DapperInteroperability/validate-local.sh
```

The example seeds isolated data for a unique run id, validates the query results in-process, and exits non-zero if any scenario does not work.
