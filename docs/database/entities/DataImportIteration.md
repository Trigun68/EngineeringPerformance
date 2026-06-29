# DataImportIteration

**Type:** Domain Entity
**Location:** `EngineeringPerformance.Infrastructure/Data/Entities/DataImportIteration.cs`
**Last Updated:** 2026-04-15

## Purpose

Tracks individual Excel import batches for Physical Volumes and Labor Intensities. Each import operation creates one iteration record, enabling versioned data management and batch-level deletion of imported records.

## Properties

| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| `Id` | `int` | No | — | Primary key |
| `ProjectId` | `int` | No | — | FK to `Project` |
| `ProjectAnalysisVersionId` | `int` | No | — | FK to `ProjectAnalysisVersion` |
| `DataType` | `DataImportType` | No | — | Enum: PhysicalVolume=1, LaborIntensity=2 (stored as int) |
| `ImportedAt` | `DateTime` | No | — | UTC timestamp of import; used as batch identifier |
| `Description` | `string?` | Yes | 500 | Optional import description |
| `RecordCount` | `int` | No | — | Number of records imported in this batch |
| `Project` | `Project` | No | — | Navigation to project |
| `ProjectAnalysisVersion` | `ProjectAnalysisVersion` | No | — | Navigation to analysis version |

## Relationships

| Navigation Property | Target | Type | FK / Key |
|---------------------|--------|------|----------|
| `Project` | `Project` | Many-to-One | `ProjectId` |
| `ProjectAnalysisVersion` | `ProjectAnalysisVersion` | Many-to-One | `ProjectAnalysisVersionId` |

## Database Constraints

- **Primary Key:** `Id`
- **Table Name:** `DataImportIterations`
- **Foreign Keys:**
  - `ProjectId` → `Projects.Id` (DeleteBehavior: `Restrict`)
  - `ProjectAnalysisVersionId` → `ProjectAnalysisVersions.Id` (DeleteBehavior: `Cascade`)
- **Indexes:**
  - `IX_DataImportIterations_ProjectAnalysisVersionId` on `ProjectAnalysisVersionId`
  - `IX_DataImportIterations_ProjectAnalysisVersionId_DataType` on composite `{ProjectAnalysisVersionId, DataType}`

## Usage Pattern

### Import Flow

```
1. Create DataImportIteration { ProjectId, ProjectAnalysisVersionId, DataType, ImportedAt = UtcNow }
2. Save iteration to get Id
3. Read Excel, create data entities (ProjectPhysicalVolume or ProjectLaborIntensity)
4. Save data entities
5. Update iteration.RecordCount
6. Commit transaction
```

### Deletion Flow

```
1. Find iteration by {ProjectId, ProjectAnalysisVersionId, DataType, ImportedAt}
2. Delete data records WHERE CreatedAt == importedAt
3. Delete iteration record
4. Commit transaction
```

### Query Pattern

```csharp
// Get all import iterations for a project/version/type
context.DataImportIterations
    .Where(i => i.ProjectId == projectId
             && i.ProjectAnalysisVersionId == versionId
             && i.DataType == dataType)
    .OrderByDescending(i => i.ImportedAt)
    .Select(i => i.ImportedAt)
```

## Batch Identification Strategy

All imported records share the same `CreatedAt` timestamp, which matches the iteration's `ImportedAt` value. This enables batch-level deletion without requiring a direct FK from data tables to the iteration table.

## Related Documentation

- [Data Import Feature](../features/data-import.md)
- [Project Modal Redesign](../features/project-modal-redesign.md)
- [Entity Relationships](../database/entity-relationships.md)
