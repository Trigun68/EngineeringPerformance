#!/usr/bin/env bash
# E2E через официальный образ Playwright (обход неподдерживаемого ubuntu26 на хосте).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
BASE_URL="${E2E_BASE_URL:-http://localhost:8081}"
IMAGE="${PLAYWRIGHT_IMAGE:-mcr.microsoft.com/playwright:v1.49.1-noble}"
# Версия образа должна совпадать с @playwright/test в package.json

echo "=== Playwright E2E (Docker) → ${BASE_URL} ==="

docker run --rm --network host \
  -v "${ROOT}:/work" -w /work \
  -e E2E_BASE_URL="${BASE_URL}" \
  -e CI=1 \
  "${IMAGE}" \
  bash -lc "npm ci --silent && npx playwright test"
