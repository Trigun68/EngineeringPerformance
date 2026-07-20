#!/usr/bin/env bash
# Smoke-проверка latency ключевых endpoint после деплоя.
set -euo pipefail

BASE_URL="${1:-http://localhost:8081}"
API="${BASE_URL}/api"

echo "=== Perf smoke: ${BASE_URL} ==="

COOKIE_JAR=$(mktemp)
trap 'rm -f "$COOKIE_JAR"' EXIT

http_code=$(curl -s -o /dev/null -w "%{http_code}" -c "$COOKIE_JAR" -X POST "${API}/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}')

if [[ "$http_code" != "200" ]]; then
  echo "FAIL login HTTP $http_code"
  exit 1
fi

CURL_AUTH=(-b "$COOKIE_JAR")

time_curl() {
  local label="$1"
  local url="$2"
  local code elapsed_ms
  local metrics
  metrics=$(curl -s -o /dev/null -w "%{http_code} %{time_total}" "${CURL_AUTH[@]}" "$url")
  code=$(echo "$metrics" | awk '{print $1}')
  elapsed_ms=$(echo "$metrics" | awk '{printf "%.0f", $2 * 1000}')
  echo "  ${label}: HTTP ${code}, ${elapsed_ms}ms"
  if [[ "$code" != "200" && "$code" != "204" ]]; then
    echo "FAIL ${label}"
    exit 1
  fi
}

echo "Endpoints:"
time_curl "projects" "${API}/projects"
time_curl "dictionaries" "${API}/dictionaries/all"
time_curl "audit" "${API}/audit/user-actions?take=50"

project_id=$(curl -s "${CURL_AUTH[@]}" "${API}/projects" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data[0]['id'] if data else '')
" 2>/dev/null || true)

if [[ -n "$project_id" ]]; then
  time_curl "versions" "${API}/projects/${project_id}/versions"
  version_id=$(curl -s "${CURL_AUTH[@]}" "${API}/projects/${project_id}/versions" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data[0]['id'] if data else '')
" 2>/dev/null || true)
  if [[ -n "$version_id" ]]; then
    time_curl "performance" "${API}/projects/${project_id}/versions/${version_id}/performance"
    time_curl "analysis-export" "${API}/projects/${project_id}/versions/${version_id}/export/analysis.xlsx"
  fi
fi

echo "=== OK ==="
