#!/usr/bin/env bash
# Smoke-проверка latency ключевых endpoint после деплоя.
#
# Каждый endpoint измеряется дважды: cold (первый вызов) и warm (повтор из кэша).
# Бюджеты задаются в мс через переменные окружения, например:
#   BUDGET_PAGE_DATA_COLD=6000 ./scripts/perf-smoke.sh http://localhost:8081
# PERF_STRICT=0 — превышение бюджета печатается как WARN и не роняет прогон.
set -euo pipefail

BASE_URL="${1:-http://localhost:8081}"
API="${BASE_URL}/api"
PERF_STRICT="${PERF_STRICT:-1}"

# Бюджеты (мс). Cold — холодный расчёт, warm — повтор (ожидается кэш, ISS-018).
BUDGET_LOGIN="${BUDGET_LOGIN:-3000}"
BUDGET_PROJECTS="${BUDGET_PROJECTS:-2000}"
BUDGET_DICTIONARIES="${BUDGET_DICTIONARIES:-3000}"
BUDGET_AUDIT="${BUDGET_AUDIT:-2000}"
BUDGET_VERSIONS="${BUDGET_VERSIONS:-1500}"
BUDGET_PERFORMANCE="${BUDGET_PERFORMANCE:-8000}"
BUDGET_PAGE_DATA_COLD="${BUDGET_PAGE_DATA_COLD:-10000}"
BUDGET_PAGE_DATA_WARM="${BUDGET_PAGE_DATA_WARM:-4000}"
BUDGET_REPORT_COLD="${BUDGET_REPORT_COLD:-10000}"
BUDGET_REPORT_WARM="${BUDGET_REPORT_WARM:-2000}"
BUDGET_EXPORT="${BUDGET_EXPORT:-15000}"

echo "=== Perf smoke: ${BASE_URL} (strict=${PERF_STRICT}) ==="

COOKIE_JAR=$(mktemp)
trap 'rm -f "$COOKIE_JAR"' EXIT

failures=0
warnings=0

login_metrics=$(curl -s -o /dev/null -w "%{http_code} %{time_total}" -c "$COOKIE_JAR" -X POST "${API}/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}')
login_code=$(echo "$login_metrics" | awk '{print $1}')
login_ms=$(echo "$login_metrics" | awk '{printf "%.0f", $2 * 1000}')

if [[ "$login_code" != "200" ]]; then
  echo "FAIL login HTTP $login_code"
  exit 1
fi

# --compressed: меряем как браузер (nginx отдаёт JSON и бандлы в gzip).
CURL_AUTH=(-b "$COOKIE_JAR" --compressed)

# Превышение бюджета: FAIL при PERF_STRICT=1, иначе WARN.
check_budget() {
  local label="$1" elapsed_ms="$2" budget_ms="$3"
  if (( elapsed_ms <= budget_ms )); then
    return 0
  fi
  if [[ "$PERF_STRICT" == "1" ]]; then
    echo "    FAIL ${label}: ${elapsed_ms}ms > бюджет ${budget_ms}ms"
    failures=$((failures + 1))
  else
    echo "    WARN ${label}: ${elapsed_ms}ms > бюджет ${budget_ms}ms"
    warnings=$((warnings + 1))
  fi
}

time_curl() {
  local label="$1" url="$2" budget="$3"
  local metrics code elapsed_ms
  metrics=$(curl -s -o /dev/null -w "%{http_code} %{time_total}" "${CURL_AUTH[@]}" "$url")
  code=$(echo "$metrics" | awk '{print $1}')
  elapsed_ms=$(echo "$metrics" | awk '{printf "%.0f", $2 * 1000}')
  echo "  ${label}: HTTP ${code}, ${elapsed_ms}ms (бюджет ${budget}ms)"
  if [[ "$code" != "200" && "$code" != "204" ]]; then
    echo "    FAIL ${label}: HTTP ${code}"
    failures=$((failures + 1))
    return 0
  fi
  check_budget "$label" "$elapsed_ms" "$budget"
}

# Cold + warm: второй вызов должен попасть в кэш аналитики (ISS-018).
time_curl_cached() {
  local label="$1" url="$2" cold_budget="$3" warm_budget="$4"
  time_curl "${label} (cold)" "$url" "$cold_budget"
  time_curl "${label} (warm)" "$url" "$warm_budget"
}

echo "Endpoints:"
echo "  login: HTTP ${login_code}, ${login_ms}ms (бюджет ${BUDGET_LOGIN}ms)"
check_budget "login" "$login_ms" "$BUDGET_LOGIN"
time_curl "projects" "${API}/projects" "$BUDGET_PROJECTS"
time_curl "dictionaries" "${API}/dictionaries/all" "$BUDGET_DICTIONARIES"
time_curl "audit" "${API}/audit/user-actions?take=50" "$BUDGET_AUDIT"

project_id=$(curl -s "${CURL_AUTH[@]}" "${API}/projects" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data[0]['id'] if data else '')
" 2>/dev/null || true)

if [[ -n "$project_id" ]]; then
  time_curl "versions" "${API}/projects/${project_id}/versions" "$BUDGET_VERSIONS"
  version_id=$(curl -s "${CURL_AUTH[@]}" "${API}/projects/${project_id}/versions" | python3 -c "
import sys, json
data = json.load(sys.stdin)
print(data[0]['id'] if data else '')
" 2>/dev/null || true)
  if [[ -n "$version_id" ]]; then
    time_curl "performance" "${API}/projects/${project_id}/versions/${version_id}/performance" "$BUDGET_PERFORMANCE"
    time_curl_cached "page-data" \
      "${API}/projects/${project_id}/versions/${version_id}/page-data" \
      "$BUDGET_PAGE_DATA_COLD" "$BUDGET_PAGE_DATA_WARM"
    time_curl_cached "performance-report" \
      "${API}/projects/${project_id}/performance-report" \
      "$BUDGET_REPORT_COLD" "$BUDGET_REPORT_WARM"
    time_curl "analysis-export" \
      "${API}/projects/${project_id}/versions/${version_id}/export/analysis.xlsx" "$BUDGET_EXPORT"
  fi
fi

if (( failures > 0 )); then
  echo "=== FAIL: превышено бюджетов/ошибок — ${failures} ==="
  exit 1
fi

if (( warnings > 0 )); then
  echo "=== OK с предупреждениями: ${warnings} ==="
else
  echo "=== OK ==="
fi
