#!/usr/bin/env bash
# Автоматическая проверка пунктов UAT (API-level smoke).
set -euo pipefail

BASE_URL="${1:-http://localhost:8081}"
API="${BASE_URL}/api"
PASS=0
FAIL=0

check() {
  local name="$1"
  local code="$2"
  if [[ "$code" == "200" || "$code" == "204" ]]; then
    echo "  OK  $name (HTTP $code)"
    PASS=$((PASS + 1))
  else
    echo "  FAIL $name (HTTP $code)"
    FAIL=$((FAIL + 1))
  fi
}

COOKIE_JAR=$(mktemp)
trap 'rm -f "$COOKIE_JAR"' EXIT

echo "=== UAT API smoke: ${BASE_URL} ==="

login_code=$(curl -s -o /dev/null -w "%{http_code}" -c "$COOKIE_JAR" -X POST "${API}/auth/login" \
  -H "Content-Type: application/json" -d '{"username":"admin","password":"admin"}')
check "JWT login" "$login_code"

CURL=(-b "$COOKIE_JAR")

check "Projects list" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/projects")"
check "Dictionaries" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/dictionaries/all")"
check "Audit log" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/audit/user-actions?take=10")"
check "Access rights" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/access-rights")"

project_id=$(curl -s "${CURL[@]}" "${API}/projects" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['id'] if d else '')" 2>/dev/null || true)

if [[ -n "$project_id" ]]; then
  check "Project versions" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/projects/${project_id}/versions")"
  version_id=$(curl -s "${CURL[@]}" "${API}/projects/${project_id}/versions" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['id'] if d else '')" 2>/dev/null || true)
  if [[ -n "$version_id" ]]; then
    check "Performance" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/projects/${project_id}/versions/${version_id}/performance")"
    check "Analysis" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/projects/${project_id}/versions/${version_id}/analysis")"
    check "Forecast" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/projects/${project_id}/versions/${version_id}/forecast")"
    check "Export analysis xlsx" "$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/projects/${project_id}/versions/${version_id}/export/analysis.xlsx")"
  fi
  versions_json=$(curl -s "${CURL[@]}" "${API}/projects/${project_id}/versions")
  v_a=$(echo "$versions_json" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['id'] if len(d)>0 else '')" 2>/dev/null || true)
  v_b=$(echo "$versions_json" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[1]['id'] if len(d)>1 else '')" 2>/dev/null || true)
  if [[ -n "$v_a" && -n "$v_b" ]]; then
    compare_code=$(curl -s -o /dev/null -w "%{http_code}" "${CURL[@]}" "${API}/projects/${project_id}/compare?versionA=${v_a}&versionB=${v_b}")
    check "Compare (summary+facility+detail)" "$compare_code"
  fi
fi

echo "---"
echo "Passed: $PASS, Failed: $FAIL"
[[ "$FAIL" -eq 0 ]]
