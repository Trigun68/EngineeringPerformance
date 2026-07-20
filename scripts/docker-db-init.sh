#!/bin/bash
set -euo pipefail

SQL_HOST="${SQL_HOST:-sqlserver}"
SQL_USER="${SQL_USER:-sa}"
SQL_PASSWORD="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"
DB_NAME="${DB_NAME:-EngineeringPerformance}"

# -b: abort on SQL error (otherwise schema/seed failures exit 0 and CI boots empty DB).
SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S "${SQL_HOST}" -U "${SQL_USER}" -P "${SQL_PASSWORD}" -C -I -b)

SCHEMA_FILES=(
  schema/001_InitialSchema.sql
  schema/002_Consolidated_20260413-14.sql
  schema/006_Consolidated_20260415.sql
  schema/007_PhysicalVolumeCoefficient.sql
  schema/008_RemoveNormValue.sql
  schema/009_DropIicWorkDirectionNormNormValue.sql
  schema/010_AddLaborIntensity.sql
  schema/011_AddProjectCharacteristics.sql
  schema/012_DropProjectCharacteristicOptionAccuracy.sql
  schema/013_AddProjectCharacteristicSelections.sql
  schema/014_AddUserSettings.sql
  schema/015_AddUserActionLogs.sql
  schema/016_ExpandLaborDocumentationCodeTo450.sql
  schema/017_SplitPhysicalVolumes.sql
  schema/017b_BackfillProjectPhysicalVolumeCatalogLinks.sql
  schema/099_WebApiSchemaPatch.sql
  schema/100_ProjectVersionPlanningParameters.sql
  schema/101_ParametricNorms.sql
  schema/102_PlanningScenarios.sql
  schema/103_AddPhysicalVolumeBerKfoLink.sql
)

DATA_FILES=(
  data/002_SeedData.sql
  data/003_SeedDataFromExcel.sql
  data/011_RefreshIicFromTexasV2.sql
  data/004_LaborIntensity.sql
  data/005_PhysicalVolumes.sql
  data/005_PhysicalVolumesKfo.sql
  data/012_RefreshPhysicalVolumesKfoFromNomenclature.sql
  data/006_PhysicalVolumesBer.sql
  data/013_RefreshPhysicalVolumesBerFromNomenclature.sql
  data/008_ProjectCharacteristics.sql
  data/009_BackfillAccessRights.sql
  data/010_EquipmentSeed.sql
  data/014_RefreshEquipmentFromNomenclature.sql
  data/016_SeedCiDemoProjects.sql
)

table_exists() {
  local table_name="$1"
  "${SQLCMD[@]}" -d "${DB_NAME}" -Q "SET NOCOUNT ON; SELECT 1 FROM sys.tables WHERE name = N'${table_name}'" \
    | grep -q "1"
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
    data/010_EquipmentSeed.sql|data/014_RefreshEquipmentFromNomenclature.sql)
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

if "${SQLCMD[@]}" -d "${DB_NAME}" -Q "SELECT 1 FROM sys.tables WHERE name = 'Users'" | grep -q "1"; then
  echo "Database exists — applying schema drift patch and idempotent seed data."
  "${SQLCMD[@]}" -d "${DB_NAME}" -i schema/017_SplitPhysicalVolumes.sql
  "${SQLCMD[@]}" -d "${DB_NAME}" -i schema/017b_BackfillProjectPhysicalVolumeCatalogLinks.sql
  "${SQLCMD[@]}" -d "${DB_NAME}" -i schema/099_WebApiSchemaPatch.sql
  "${SQLCMD[@]}" -d "${DB_NAME}" -i schema/100_ProjectVersionPlanningParameters.sql
  "${SQLCMD[@]}" -d "${DB_NAME}" -i schema/101_ParametricNorms.sql
  "${SQLCMD[@]}" -d "${DB_NAME}" -i schema/102_PlanningScenarios.sql
  "${SQLCMD[@]}" -d "${DB_NAME}" -i schema/103_AddPhysicalVolumeBerKfoLink.sql
  echo ">>> Re-applying seed data scripts (idempotent)..."
  for file in "${DATA_FILES[@]}"; do
    run_seed_file "${file}"
  done
  exit 0
fi

echo ">>> Applying schema scripts..."
for file in "${SCHEMA_FILES[@]}"; do
  echo "  - ${file}"
  "${SQLCMD[@]}" -d "${DB_NAME}" -i "${file}"
done

echo ">>> Applying seed data scripts..."
for file in "${DATA_FILES[@]}"; do
  run_seed_file "${file}"
done

echo "Database initialized successfully."
