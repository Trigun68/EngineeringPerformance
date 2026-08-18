#!/usr/bin/env bash
# E2E через официальный образ Playwright (обход неподдерживаемого ubuntu26 на хосте).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "${ROOT}/.." && pwd)"
BASE_URL="${E2E_BASE_URL:-http://localhost:8081}"
IMAGE="${PLAYWRIGHT_IMAGE:-mcr.microsoft.com/playwright:v1.49.1-noble}"
# Версия образа должна совпадать с @playwright/test в package.json
PLAYWRIGHT_ARGS="${*:-}"

echo "=== Playwright E2E (Docker) → ${BASE_URL} ==="
if [[ -n "${PLAYWRIGHT_ARGS}" ]]; then
  echo "Specs: ${PLAYWRIGHT_ARGS}"
fi

# --ipc=host обязателен для Chromium: по умолчанию контейнеру достаётся /dev/shm 64 МБ,
# на тяжёлых гридах (прогноз/факт — десятки тысяч строк) вкладка падает с «Target crashed»,
# и тест валится не по своей причине, а на login в beforeEach.
docker run --rm --network host --ipc=host \
  -v "${ROOT}:/work" -w /work \
  -v "${REPO_ROOT}/.src:/samples:ro" \
  -e E2E_BASE_URL="${BASE_URL}" \
  -e E2E_SAMPLES_DIR="/samples" \
  -e CI=1 \
  "${IMAGE}" \
  bash -lc "npm ci --silent && npx playwright test ${PLAYWRIGHT_ARGS}"
