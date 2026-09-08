#!/bin/bash
set -euo pipefail

SQL_HOST="${SQL_HOST:-sqlserver}"
SQL_USER="${SQL_USER:-sa}"
SQL_PASSWORD="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"
DB_NAME="${DB_NAME:-EngineeringPerformance}"

# -b: abort on SQL error (otherwise schema/seed failures exit 0 and CI boots empty DB).
# -I: QUOTED_IDENTIFIER ON (filtered indexes и прочий DDL).
SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S "${SQL_HOST}" -U "${SQL_USER}" -P "${SQL_PASSWORD}" -C -I -b)

DATA_FILES=(
  data/002_SeedData.sql
  data/003_SeedDataFromExcel.sql
  data/011_RefreshIicFromTexasV2.sql
  data/022_IicNormsEpms20260701.sql
  data/004_LaborIntensity.sql
  data/024_BackfillLaborIntensityDocumentTypeName.sql
  data/005_PhysicalVolumes.sql
  data/005_PhysicalVolumesKfo.sql
  data/012_RefreshPhysicalVolumesKfoFromNomenclature.sql
  data/006_PhysicalVolumesBer.sql
  data/013_RefreshPhysicalVolumesBerFromNomenclature.sql
  data/008_ProjectCharacteristics.sql
  data/009_BackfillAccessRights.sql
  data/010_EquipmentSeed.sql
  data/014_RefreshEquipmentFromNomenclature.sql
  data/023_UnarchiveEquipmentAutoCreatedBy25970Import.sql
  data/016_SeedCiDemoProjects.sql
)

table_exists() {
  local table_name="$1"
  "${SQLCMD[@]}" -d "${DB_NAME}" -Q "SET NOCOUNT ON; SELECT 1 FROM sys.tables WHERE name = N'${table_name}'" \
    | grep -q "1"
}

schema_migration_applied() {
  local script_name="$1"
  "${SQLCMD[@]}" -d "${DB_NAME}" -Q "SET NOCOUNT ON; SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = N'${script_name}'" \
    | grep -q "1"
}

stamp_schema_migration() {
  local script_name="$1"
  "${SQLCMD[@]}" -d "${DB_NAME}" -Q "SET NOCOUNT ON; IF NOT EXISTS (SELECT 1 FROM dbo.SchemaMigrations WHERE ScriptName = N'${script_name}') INSERT INTO dbo.SchemaMigrations (ScriptName) VALUES (N'${script_name}');"
}

# C-locale: 016 → 017_ → 017a → 017b (как исторический SCHEMA_FILES).
sorted_schema_files() {
  LC_ALL=C printf '%s\n' schema/*.sql | LC_ALL=C sort
}

# Имя сортируется строго до «017_» в C-locale (001–016). Не [[ a < b ]]:
# префикс LC_ALL=… делает [[ обычной командой, и < становится редиректом.
schema_name_before_017() {
  local name="$1"
  local first
  # Без head: pipefail + SIGPIPE от закрытого пайпа роняет init.
  first="$(printf '%s\n%s\n' "${name}" '017_' | LC_ALL=C sort)"
  first="${first%%$'\n'*}"
  [[ "${first}" == "${name}" && "${name}" != "017_" ]]
}

# Существующая БД: 001–016 уже применены (014 — голый CREATE TABLE, повтор опасен).
# Штампуем все schema-файлы, чьи имена в C-locale меньше «017_», кроме 000.
# 017+ не штампуем без прогона — они идемпотентны и могут отсутствовать на окружении.
stamp_baseline_schema_if_needed() {
  local file basename did_stamp=0
  while IFS= read -r file; do
    basename="${file##*/}"
    [[ "${basename}" == "000_SchemaMigrations.sql" ]] && continue
    schema_name_before_017 "${basename}" || break
    if schema_migration_applied "${basename}"; then
      continue
    fi
    if [[ "${did_stamp}" -eq 0 ]]; then
      echo ">>> Stamping baseline schema (001–016) on existing database — not replaying..."
      did_stamp=1
    fi
    echo "  - ${basename} (stamped)"
    stamp_schema_migration "${basename}"
  done < <(sorted_schema_files)
}

apply_schema_scripts() {
  local file basename
  echo ">>> Applying schema scripts..."
  while IFS= read -r file; do
    basename="${file##*/}"
    case "${basename}" in
      README.md)
        continue
        ;;
      *017a*)
        echo "  - ${file} (skipped: manual recovery)"
        continue
        ;;
    esac
    if schema_migration_applied "${basename}"; then
      echo "  - ${file} (already applied)"
      continue
    fi
    echo "  - ${file}"
    "${SQLCMD[@]}" -d "${DB_NAME}" -i "${file}"
    stamp_schema_migration "${basename}"
  done < <(sorted_schema_files)
}

run_seed_file() {
  local file="$1"
  case "${file}" in
    data/005_PhysicalVolumes.sql)
      if table_exists "PhysicalVolumes"; then
        echo "  - ${file}"
        "${SQLCMD[@]}" -d "${DB_NAME}" -i "${file}"
      else
        echo "  - ${file} (skipped: legacy table PhysicalVolumes removed by schema 017)"
      fi
      ;;
    data/005_PhysicalVolumesKfo.sql|data/012_RefreshPhysicalVolumesKfoFromNomenclature.sql)
      if table_exists "PhysicalVolumesKfo"; then
        echo "  - ${file}"
        "${SQLCMD[@]}" -d "${DB_NAME}" -i "${file}"
      else
        echo "  - ${file} (skipped: table PhysicalVolumesKfo not found)"
      fi
      ;;
    data/006_PhysicalVolumesBer.sql|data/013_RefreshPhysicalVolumesBerFromNomenclature.sql)
      if table_exists "PhysicalVolumesBer"; then
        echo "  - ${file}"
        "${SQLCMD[@]}" -d "${DB_NAME}" -i "${file}"
      else
        echo "  - ${file} (skipped: table PhysicalVolumesBer not found)"
      fi
      ;;
    data/010_EquipmentSeed.sql|data/014_RefreshEquipmentFromNomenclature.sql|data/023_UnarchiveEquipmentAutoCreatedBy25970Import.sql)
      if table_exists "Equipments"; then
        echo "  - ${file}"
        "${SQLCMD[@]}" -d "${DB_NAME}" -i "${file}"
      else
        echo "  - ${file} (skipped: table Equipments not found)"
      fi
      ;;
    *)
      echo "  - ${file}"
      "${SQLCMD[@]}" -d "${DB_NAME}" -i "${file}"
      ;;
  esac
}

echo "=== Engineering Performance — Docker DB Init ==="
echo "Server: ${SQL_HOST}, Database: ${DB_NAME}"

for i in $(seq 1 30); do
  if "${SQLCMD[@]}" -Q "SELECT 1" >/dev/null 2>&1; then
    break
  fi
  echo "Waiting for SQL Server (${i}/30)..."
  sleep 2
done

"${SQLCMD[@]}" -Q "IF DB_ID('${DB_NAME}') IS NULL CREATE DATABASE [${DB_NAME}];"

cd /migrations

echo ">>> Ensuring SchemaMigrations ledger..."
"${SQLCMD[@]}" -d "${DB_NAME}" -i schema/000_SchemaMigrations.sql
stamp_schema_migration "000_SchemaMigrations.sql"

existing_db=0
if table_exists "Users"; then
  existing_db=1
  echo "Database exists — applying pending schema scripts and idempotent seed data."
  stamp_baseline_schema_if_needed
else
  echo "Fresh database — applying full schema chain."
fi

apply_schema_scripts

echo ">>> Applying seed data scripts..."
for file in "${DATA_FILES[@]}"; do
  if [[ "${existing_db}" -eq 1 ]]; then
    # Не воскрешать CI demo-проекты на существующей рабочей БД после ручного удаления.
    if [[ "${file}" == "data/016_SeedCiDemoProjects.sql" ]]; then
      echo "  - ${file} (skipped on existing DB)"
      continue
    fi
    # Bulk-refresh номенклатур/норм затирают UI-правки (Coefficient, нормы ИИС) и часто
    # не идемпотентны при unique-индексах (sqlcmd -b → abort всего init).
    # Принудительно: FORCE_NOMENCLATURE_REFRESH=1.
    if [[ "${file}" == "data/011_RefreshIicFromTexasV2.sql" \
       || "${file}" == "data/022_IicNormsEpms20260701.sql" \
       || "${file}" == "data/012_RefreshPhysicalVolumesKfoFromNomenclature.sql" \
       || "${file}" == "data/013_RefreshPhysicalVolumesBerFromNomenclature.sql" \
       || "${file}" == "data/014_RefreshEquipmentFromNomenclature.sql" ]]; then
      if [[ "${FORCE_NOMENCLATURE_REFRESH:-0}" == "1" ]]; then
        echo "  - ${file} (FORCE_NOMENCLATURE_REFRESH=1)"
      else
        echo "  - ${file} (skipped on existing DB — preserves UI edits; set FORCE_NOMENCLATURE_REFRESH=1 to re-apply)"
        continue
      fi
    fi
  fi
  run_seed_file "${file}"
done

# Повторно: backfill ProjectId после появления ProjectEquipments из сидов
# (на пустой схеме первый проход 106 ещё не находит потребителей).
"${SQLCMD[@]}" -d "${DB_NAME}" -i schema/106_EquipmentProjectId.sql

if [[ "${existing_db}" -eq 0 ]]; then
  echo "Database initialized successfully."
fi
