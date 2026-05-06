# Data Import

**Status:** ✅ Implemented
**Date:** 2026-04-15

## Description

Excel-based import and export for Physical Volumes and Labor Intensities data, with iteration tracking for versioned data management.

## Import Types

| Enum Value | Integer | Target Table |
|------------|---------|-------------|
| `PhysicalVolume` | 1 | `ProjectPhysicalVolumes` |
| `LaborIntensity` | 2 | `ProjectLaborIntensities` |

## Physical Volume Import

### Excel Column Mapping

| Column # | Header Concept | Entity Property | Notes |
|----------|---------------|-----------------|-------|
| 6 | EP Code | `PhysicalVolumeId` (via lookup) | Must exist in `PhysicalVolumes.Code` catalog |
| 7 | EP Name | — | Display only, not persisted |
| 8 | Quantity | `Quantity` | Parsed as decimal |
| 9 | Facility Code | `FacilityCode` | Nullable string |
| 10 | Facility Name | `FacilityName` | Nullable string |
| 11 | Binding Type | `BindingType` | Nullable string |
| 12 | Document Set | `DocumentSet` | Nullable string |
| 13 | Catalog ID | `CatalogId` | Nullable string |
| 14 | Tag | `Tag` | Nullable string |
| 15 | Work Comment | `WorkComment` | Nullable string |
| 16 | Estimation Basis | `EstimationBasis` | Nullable string |

### Validation Rules

- Rows with empty EP Code are skipped
- Rows where EP Code is not found in the `PhysicalVolumes` catalog are skipped (warning logged)
- First row is treated as header and skipped

**Common “0 rows imported” cause:** the work codes in the BoQ file (e.g. `13-06-001-01` or `01-01-001-01` from «Список работ») must exist as `PhysicalVolumes.Code`. Demo seeds may only contain codes like `FO-001`; then every row is skipped until the full catalog (e.g. from «Физические объёмы» / migration `migrations/data/005_PhysicalVolumes.sql`) is loaded.

When import finishes with zero rows, the app shows a detailed breakdown (rows read, catalog size, rows without code vs not in catalog).

### Process Flow

1. Create `DataImportIteration` record with `ImportedAt = DateTime.UtcNow`
2. Load all `PhysicalVolumes` into dictionary keyed by `Code`
3. Read Excel rows starting from row 2
4. For each valid row, create `ProjectPhysicalVolume` entity
5. Save all entities in a database transaction
6. Update iteration `RecordCount`

## Labor Intensity Import

### Excel Column Mapping

| Column # | Header Concept | Entity Property | Notes |
|----------|---------------|-----------------|-------|
| 1 | Documentation Code | `DocumentationCode` | Auto-generated if missing: `LI-{projectId}-{index}` |
| 3 | Brand | `Brand` | Nullable string |
| 4 | Work Direction Name | `WorkDirectionId` (via lookup) | Must exist in `IicWorkDirections.Name` (case-insensitive) |
| 5 | Planned Hours | `PlannedHours` | Parsed as decimal |
| 6 | Actual Hours | `ActualHours` | Parsed as decimal |
| 7 | Notes | `Notes` | Nullable string |

### Validation Rules

- Rows with empty Work Direction Name are skipped
- Rows where Work Direction Name is not found in the `IicWorkDirections` catalog are skipped (warning logged)
- Missing `DocumentationCode` is auto-generated as `LI-{projectId}-{index}`
- First row is treated as header and skipped

### Process Flow

1. Create `DataImportIteration` record with `ImportedAt = DateTime.UtcNow`
2. Load all `IicWorkDirections` into dictionary keyed by `Name` (case-insensitive)
3. Read Excel rows starting from row 2
4. For each valid row, create `ProjectLaborIntensity` entity
5. Save all entities in a database transaction
6. Update iteration `RecordCount`

## Iteration Deletion

Each import creates a batch identified by its `ImportedAt` timestamp. Deletion removes all records from a specific import batch:

1. Find `DataImportIteration` by `projectId`, `versionId`, `dataType`, and `importedAt`
2. Delete all `ProjectPhysicalVolume` or `ProjectLaborIntensity` records where `CreatedAt == importedAt`
3. Remove the iteration record
4. All operations within a single database transaction

## ViewModel Commands

| Command | Type | Requirement |
|---------|------|-------------|
| `ImportPhysicalVolumesCommand` | Import | `SelectedVersion` and `EntityId` must be set |
| `DeletePhysicalVolumesCommand` | Delete | `SelectedVersion` and `EntityId` must be set |
| `ImportLaborIntensitiesCommand` | Import | `SelectedVersion` and `EntityId` must be set |
| `DeleteLaborIntensitiesCommand` | Delete | `SelectedVersion` and `EntityId` must be set |

After successful import or delete, performance data is reloaded automatically.

## Dependencies

| Library | Usage |
|---------|-------|
| ClosedXML (`XLWorkbook`) | Excel file reading |
| Serilog (`Log`) | Warning/error logging for skipped rows |

## Related Documentation

- [Project Modal Redesign](./project-modal-redesign.md)
- [DataImportIteration Entity](../database/entities/DataImportIteration.md)
- [Performance Analytics](./performance-analytics.md)
