# Database Migration

## Overview

This document describes the database migration setup for the Engineering Performance Management System.

## Migration folder layout

SQL scripts are grouped by purpose. **Entry points** (run with `sqlcmd`; relative paths in `:r` resolve from the directory of the script file that contains each `:r`):

| File | Purpose |
|------|---------|
| `migrations/000_MasterMigration.sql` | Full deployment: structure + data (includes both parts below) |
| `migrations/FullMigration_01_Structure.sql` | Part 1 of 2 — DDL only (`001` + `006` + `007`) |
| `migrations/FullMigration_02_DataImport.sql` | Part 2 of 2 — data scripts (`002` → `005`) |

| Subfolder | Contents |
|-----------|----------|
| `migrations/schema/` | `001_InitialSchema.sql`, `006_Consolidated_20260415.sql`, `007_PhysicalVolumeCoefficient.sql` |
| `migrations/data/` | `002_SeedData.sql`, `003_SeedDataFromExcel.sql` (placeholder), `004_LaborIntensity.sql`, `005_PhysicalVolumes.sql` |

**Generators** (regenerate large seeds from Excel):

| Script | Output |
|--------|--------|
| `migrations/tools/generate_fizobem_physical_volumes_seed.py` | `migrations/data/005_PhysicalVolumes.sql` (лист «Физ.объем») |
| `migrations/tools/generate_iic_texas_seed.py` | fragments / IIC from «Техас» (used alongside `002`) |

See also: `_doc/deployment/DEPLOY.md` (Russian deployment guide).

## Source of truth and legacy drift (2026-06)

**Production schema is defined by EF Core migrations** in `EngineeringPerformance.Infrastructure/Migrations/`.
The application applies them at startup (`DatabaseSeeder.MigrateAsync`) or via `dotnet ef database update`.

**Legacy `sqlcmd` scripts** under `migrations/schema/` and `migrations/data/` are **not kept in sync automatically**.
As of the latest EF migration (`ExpandEquipmentTextFields`, 2026-06-01), the SQL script chain ends at `schema/016_ExpandLaborDocumentationCodeTo450.sql` and **does not include**:

| Missing in sqlcmd chain | EF migration (examples) |
|-------------------------|-------------------------|
| `Equipments`, `ProjectEquipments` | `AddEquipment`, `AddEquipmentUnitId`, `FixEquipmentCatalogKeyIndex`, `ExpandEquipmentTextFields` |
| V2 additive fields, snapshots, permissions | `V2A_*`, `V2B_AnalysisVersionSnapshot`, `V2C_ProjectPermissions`, `V2D_*` |
| User settings, action logs (partial) | `AddUserSettings`, `AddUserActionLogs` (scripts `014`–`015` exist but master chain may omit them) |
| Post-April indexes and constraint changes | multiple migrations after `006_Consolidated` |

**Recommendation:** use EF migrations for all new environments. Use `sqlcmd` only when .NET SDK is unavailable; after any schema change, regenerate scripts with `scripts/Regenerate-SqlSchema.ps1` or extend the master chain manually — never mix EF-applied DB with partial sqlcmd updates.

## Migration Files

### Location

- **EF Core migrations**: `EngineeringPerformance.Infrastructure/Migrations/*.cs`
- **SQL — initial schema**: `migrations/schema/001_InitialSchema.sql`
- **SQL — consolidated DDL (2026-04-15)**: `migrations/schema/006_Consolidated_20260415.sql`
- **SQL — `PhysicalVolumes.Coefficient`**: `migrations/schema/007_PhysicalVolumeCoefficient.sql` (EF: `20260419213543_AddPhysicalVolumeCoefficient`)
- **Seed Data**: `migrations/data/002_SeedData.sql`
- **Placeholder**: `migrations/data/003_SeedDataFromExcel.sql` (no inserts; reserved for future Excel-only extras)
- **Model Snapshot**: `EngineeringPerformance.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`

### Design-Time Support

`AppDbContextFactory.cs` implements `IDesignTimeDbContextFactory<AppDbContext>` to enable EF Core CLI tools. It reads the connection string from `AppSettings`.

## Tables Created

The initial migration creates the core tables; later DDL adds columns and tables (see `006` and `007`). Main entities:

| Table | Description |
|-------|-------------|
| `Roles` | User roles |
| `Users` | User accounts with authentication |
| `AccessRights` | Role-based access control |
| `Projects` | Project definitions with metadata |
| `ProjectAnalysisVersions` | Project analysis versions (plan / fact / forecast) |
| `ProjectUserAccess` | User–project access mapping |
| `PhysicalVolumeTypes` | Types of physical volumes (БЭР, КФО, …) |
| `PhysicalVolumes` | Physical volume catalog (includes `Coefficient` after `007`) |
| `IicWorkDirections` | Work direction classifier (IIC) |
| `IicWorkDirectionNorms` | Norms register |
| `UnitsOfMeasurement` | Units dictionary |
| `ProjectPhysicalVolumes` | Per-version quantities (`Quantity` in catalog units; × `Coefficient` → IIC group units in analytics) |
| `ProjectLaborIntensities` | Labor intensity per version |
| `DataImportIterations` | Excel import batch tracking (from `006`) |

## Seed Data (`002_SeedData.sql`)

The `migrations/data/002_SeedData.sql` script loads **reference and demo** data. It is **idempotent** — safe to run multiple times.

### What gets seeded (summary)

| Area | Contents |
|------|----------|
| Roles / Users | Administrator, ProjectManager, Observer; users `admin`, `manager`, `observer`, optional `eliseev` (passwords documented in script header) |
| Units | м³, м², шт, т, кг, час, ярд³, компл., км, м, га, п.м. |
| Physical volume types | БЭР, КФО |
| IIC work directions | Full tree from «Техас» sheet (`Копия ++++-1.xlsx`), codes `IIC-*` |
| IIC norms | Norms per direction (performance / estimate / std dev where applicable) |
| Projects | Не сидируются (создаются в UI) |
| Access rights | Edit (PM) + Read (Observer) for objects in `AccessControlConstants` |

### How to apply `002` only

Run **after** schema migrations (`FullMigration_01` or at least `001` + `006` + `007`):

```bash
cd migrations
sqlcmd -S <server> -d <database> -i data/002_SeedData.sql
```

### Chain `003` … `005`

| File | Role |
|------|------|
| `003_SeedDataFromExcel.sql` | No data inserts; keeps ordering in `FullMigration_02`. |
| `004_LaborIntensity.sql` | LI-001 … LI-033 under root `IIC`. |
| `005_PhysicalVolumes.sql` | Full nomenclature of physical volumes; regenerate via `generate_fizobem_physical_volumes_seed.py`. |

Run order is fixed in `FullMigration_02_DataImport.sql` (`002` → `003` → `004` → `005`).

## How to Apply

### Option 1: EF Core CLI (Development)

```bash
cd EngineeringPerformance.Infrastructure
dotnet ef database update
```

### Option 2: SQL Script (Production)

**Recommended:** run the full pipeline with `sqlcmd` (paths in `:r` are resolved relative to each including script’s directory):

```bash
cd migrations
sqlcmd -S <server> -d <database> -i 000_MasterMigration.sql
```

To deploy **structure only**, then **data** in a second step:

```bash
cd migrations
sqlcmd -S <server> -d <database> -i FullMigration_01_Structure.sql
sqlcmd -S <server> -d <database> -i FullMigration_02_DataImport.sql
```

To apply only the initial EF-generated schema (without consolidated 2026-04-15 DDL):

```bash
cd migrations
sqlcmd -S <server> -d <database> -i schema/001_InitialSchema.sql
```

Or open the `.sql` files in SSMS and execute (again, prefer setting the working context so includes work if you use the wrapper scripts).

`schema/001_InitialSchema.sql` is **idempotent** — it checks `__EFMigrationsHistory` before creating each table, so it's safe to run multiple times.

## How to Generate New Migrations

```bash
# Create a new migration
cd EngineeringPerformance.Infrastructure
dotnet ef migrations add <MigrationName>

# Generate SQL script for all pending migrations
dotnet ef migrations script --output ../migrations/<name>.sql --idempotent

# Generate SQL for a specific migration range
dotnet ef migrations script <from> <to> --output ../migrations/<name>.sql
```

## Migration History

EF Core tracks applied migrations in the `__EFMigrationsHistory` table. Each entry records:
- `MigrationId`: Migration name (e.g., `20260411092503_InitialSchema`)
- `ProductVersion`: EF Core version (e.g., `10.0.5`)

### `20260415193900_AddPhysicalVolumeRelationships`

**Date:** 2026-04-15
**Purpose:** Establish relationships between `PhysicalVolume`, `ProjectPhysicalVolume`, and `ProjectLaborIntensity`.

**Changes:**

| Operation | Table | Detail |
|-----------|-------|--------|
| Add column | `ProjectLaborIntensities` | `PhysicalVolumeId` — `int`, NULLABLE |
| Add column | `PhysicalVolumes` | `DocumentSet` — `nvarchar(100)`, NULLABLE |
| Add index | `ProjectPhysicalVolumes` | `IX_ProjectPhysicalVolumes_CatalogId` |
| Add index | `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_Brand` |
| Add index | `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_PhysicalVolumeId` |
| Add foreign key | `ProjectLaborIntensities` | `PhysicalVolumeId` → `PhysicalVolumes.Id` (`ON DELETE RESTRICT`) |

**EF Core Configuration Changes:**

- `PhysicalVolumeConfiguration`: Added `DocumentSet` property mapping, `HasMany(pv => pv.LaborIntensities)` relationship
- `ProjectPhysicalVolumeConfiguration`: Updated `WithMany(pv => pv.ProjectPhysicalVolumes)` with named inverse, added index on `CatalogId`
- `ProjectLaborIntensityConfiguration`: Added `PhysicalVolume` relationship with `HasForeignKey(li => li.PhysicalVolumeId)` / `HasPrincipalKey(pv => pv.Id)`, indexes on `Brand` and `PhysicalVolumeId`

**Relationships:**

1. **PhysicalVolume ↔ ProjectPhysicalVolume**: Standard FK via `PhysicalVolumeId` (database-enforced). Business-level matching: `PhysicalVolume.Code` ↔ `ProjectPhysicalVolume.CatalogId` (logical, for data reconciliation).
2. **PhysicalVolume ↔ ProjectLaborIntensity**: Standard FK via `PhysicalVolumeId` (database-enforced). Business-level matching: `PhysicalVolume.DocumentSet` ↔ `ProjectLaborIntensity.Brand` (logical, for data reconciliation).

**Related Documentation:**
- [Entity Relationships](./entity-relationships.md)
- [PhysicalVolume](./entities/PhysicalVolume.md)
- [ProjectPhysicalVolume](./entities/ProjectPhysicalVolume.md)
- [ProjectLaborIntensity](./entities/ProjectLaborIntensity.md)

### `006_Consolidated_20260415.sql` (`migrations/schema/`)

**Date:** 2026-04-15
**Purpose:** Consolidates 4 EF Core migrations from 2026-04-15 into a single idempotent SQL script for production deployment.
**Baseline:** `20260414214934_AddProjectPhysicalVolumeDetailsAndLaborIntensityBrand`

This file is a hand-crafted idempotent migration that combines four separate EF Core migrations into one script. It is **safe to run multiple times** — every operation is guarded by existence checks, and `__EFMigrationsHistory` entries are inserted only if not already present.

#### Why a Consolidated Script?

Running four separate migrations in production increases deployment risk and time. The consolidated script:
- Reduces the number of round-trips to the database
- Provides clear `PRINT` output for each operation so you can see exactly what was applied
- Is idempotent — can be re-run if a partial failure occurs
- Records all four individual migration IDs in `__EFMigrationsHistory` so EF Core CLI remains in sync

#### Included Migrations

| # | EF Core Migration | Description |
|---|-------------------|-------------|
| 1 | `20260415181235_AddIicNormPerformanceFields` | Adds `Performance`, `Estimate`, `StandardDeviation` columns to `IicWorkDirectionNorms` |
| 2 | `20260415183559_AddAnalysisDirectionAndVolumeTypeToProjectAnalysisVersion` | Adds `Direction` (int, default 1) and `PhysicalVolumeTypeId` (FK) to `ProjectAnalysisVersions` |
| 3 | `20260415193900_AddPhysicalVolumeRelationships` | Adds `PhysicalVolumeId` FK to `ProjectLaborIntensities`, `DocumentSet` to `PhysicalVolumes`, and supporting indexes |
| 4 | `20260415202426_AddDataImportIterations` | Creates `DataImportIterations` table for tracking Excel import iterations |

#### Detailed Changes

**Migration 1 — IicWorkDirectionNorms Performance Fields**

| Operation | Table | Detail |
|-----------|-------|--------|
| Add column | `IicWorkDirectionNorms` | `Performance` — `decimal(18,4)`, NULLABLE |
| Add column | `IicWorkDirectionNorms` | `Estimate` — `decimal(18,4)`, NULLABLE |
| Add column | `IicWorkDirectionNorms` | `StandardDeviation` — `decimal(18,4)`, NULLABLE |

**Migration 2 — ProjectAnalysisVersion Direction and Volume Type**

| Operation | Table | Detail |
|-----------|-------|--------|
| Add column | `ProjectAnalysisVersions` | `Direction` — `int`, NOT NULL, DEFAULT 1 |
| Add column | `ProjectAnalysisVersions` | `PhysicalVolumeTypeId` — `int`, NULLABLE |
| Add index | `ProjectAnalysisVersions` | `IX_ProjectAnalysisVersions_PhysicalVolumeTypeId` |
| Add foreign key | `ProjectAnalysisVersions` | `PhysicalVolumeTypeId` → `PhysicalVolumeTypes.Id` (`ON DELETE SET NULL`) |

**Migration 3 — Physical Volume Relationships**

| Operation | Table | Detail |
|-----------|-------|--------|
| Add column | `ProjectLaborIntensities` | `PhysicalVolumeId` — `int`, NULLABLE |
| Add column | `PhysicalVolumes` | `DocumentSet` — `nvarchar(100)`, NULLABLE |
| Add index | `ProjectPhysicalVolumes` | `IX_ProjectPhysicalVolumes_CatalogId` |
| Add index | `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_Brand` |
| Add index | `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_PhysicalVolumeId` |
| Add index | `PhysicalVolumes` | `IX_PhysicalVolumes_DocumentSet` |
| Add foreign key | `ProjectLaborIntensities` | `PhysicalVolumeId` → `PhysicalVolumes.Id` |

**Migration 4 — Data Import Iterations Table**

| Operation | Table | Detail |
|-----------|-------|--------|
| Create table | `DataImportIterations` | `Id` (PK, identity), `ProjectId` (FK → `Projects`), `ProjectAnalysisVersionId` (FK → `ProjectAnalysisVersions`, CASCADE delete), `DataType` (int), `ImportedAt` (datetime2), `Description` (nvarchar(500)), `RecordCount` (int) |
| Add index | `DataImportIterations` | `IX_DataImportIterations_ProjectAnalysisVersionId` |
| Add index | `DataImportIterations` | `IX_DataImportIterations_ProjectAnalysisVersionId_DataType` (composite) |
| Add index | `DataImportIterations` | `IX_DataImportIterations_ProjectId` |

#### How to Apply

Normally this file is applied via `FullMigration_01_Structure.sql` or `000_MasterMigration.sql`. To run it alone:

```bash
cd migrations
sqlcmd -S <server> -d <database> -i schema/006_Consolidated_20260415.sql
```

Or open `migrations/schema/006_Consolidated_20260415.sql` in SSMS and execute.

The script prints a status line for each operation (e.g., `Added column Performance to IicWorkDirectionNorms` or `Column Performance already exists in IicWorkDirectionNorms`) so you can verify what was applied.

#### EF Core Compatibility

After running this script, the `__EFMigrationsHistory` table will contain entries for all four individual migrations. This means:
- `dotnet ef database update` will recognize these migrations as already applied
- No conflict between the consolidated script and future EF Core CLI operations
- The individual EF Core migration files in `EngineeringPerformance.Infrastructure/Migrations/` can remain as-is for development reference

### `007_PhysicalVolumeCoefficient.sql` (`migrations/schema/`)

**Date:** 2026-04-19 (aligned with EF migration `20260419213543_AddPhysicalVolumeCoefficient`)

**Purpose:** Adds `Coefficient` to `PhysicalVolumes` — multiplier that converts stored project quantity (`ProjectPhysicalVolumes.Quantity`) into **IIC group units** for performance and forecast analytics (`Quantity × Coefficient`). Default `1`; seeds set `1` for existing nomenclature rows.

**Operations:**

| Operation | Table | Detail |
|-----------|-------|--------|
| Add column | `PhysicalVolumes` | `Coefficient` — `decimal(18,4)` NOT NULL, default `1` |
| Insert | `__EFMigrationsHistory` | `20260419213543_AddPhysicalVolumeCoefficient` |

Applied automatically via `FullMigration_01_Structure.sql` after `006`. For production SQL-only deploys, run the full part 1 chain so `007` is not skipped.

**Related:** [PhysicalVolume entity](./entities/PhysicalVolume.md), `PerformanceService`, `ProjectForecastService`, `ForecastPlannedVolumePersistenceService` (planned volumes in UI are in IIC units; persistence divides by `Coefficient` when writing `Quantity`).
