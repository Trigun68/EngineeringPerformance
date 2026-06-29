#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WEB_URL="${WEB_URL:-http://localhost:8081}"
RUN_SMOKE="${RUN_SMOKE:-1}"

cd "${ROOT_DIR}"

if [[ ! -f ".env" ]]; then
  cp .env.example .env
fi

echo "===> Starting docker stack"
docker compose up -d --wait

echo "===> Running backend API tests"
dotnet test backend/EngineeringPerformance.Api.Tests/EngineeringPerformance.Api.Tests.csproj --verbosity minimal

echo "===> Running backend infrastructure tests"
dotnet test backend/EngineeringPerformance.Infrastructure.Tests/EngineeringPerformance.Infrastructure.Tests.csproj --verbosity minimal

echo "===> Running client unit tests"
cd client
npm ci
npm run test

echo "===> Building client"
npm run build
cd "${ROOT_DIR}"

if [[ "${RUN_SMOKE}" == "1" ]]; then
  echo "===> Running smoke suites"
  ./scripts/uat-smoke.sh "${WEB_URL}"
  ./scripts/perf-smoke.sh "${WEB_URL}"
fi

echo "Quality gates completed successfully."
