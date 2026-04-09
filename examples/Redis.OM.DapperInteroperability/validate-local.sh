#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT_PATH="$ROOT_DIR/examples/Redis.OM.DapperInteroperability/Redis.OM.DapperInteroperability.csproj"
REDIS_URL="${REDIS_OM_EXAMPLE_REDIS_URL:-redis://localhost:6379}"

echo "Validating Dapper interoperability example against ${REDIS_URL}"

dotnet run --project "$PROJECT_PATH"
