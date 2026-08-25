# ProjectLaborIntensity

**Type:** Data Entity (inherits from `DataTableRecord`)
**Location:** `EngineeringPerformance.Infrastructure/Data/Entities/ProjectLaborIntensity.cs`
**Last Updated:** 2026-08-23

## Purpose

Represents project-specific labor intensity data — planned labor hours associated with work directions within a project and analysis version.

`PlannedHours` — единственный источник часов **факта** для Z и KPI (`CalculatePerformance`, отчёт, аналитика).  
`ActualHours` хранится (ввод/импорт), на web-оценке **не используется**.

Архитектура: [_doc/architecture/hours-and-z-ssot.md](../../../_doc/architecture/hours-and-z-ssot.md).

## Properties

| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| `Id` | `int` | No | — | Primary key (inherited from `DataTableRecord`) |
| `ProjectAnalysisVersionId` | `int` | No | — | FK to `ProjectAnalysisVersion` (inherited) |
| `ProjectId` | `int` | No | — | FK to `Project` |
| `DocumentationCode` | `string` | No | 100 | Documentation code |
| `WorkDirectionId` | `int` | No | — | FK to `IicWorkDirection` |
| `PlannedHours` | `decimal` | No | precision(18,4) | Planned labor hours |
| `ActualHours` | `decimal` | No | precision(18,4) | Actual labor hours |
| `Notes` | `string?` | Yes | 2000 | Notes |
| `Brand` | `string?` | Yes | 100 | Brand/marque — business-level match with `PhysicalVolume.DocumentSet` |
| `PhysicalVolumeId` | `int?` | Yes | — | FK to `PhysicalVolume` |
| `CreatedAt` | `DateTime` | No | — | Creation timestamp (inherited) |
| `ModifiedAt` | `DateTime?` | Yes | — | Last modification timestamp (inherited) |

## Relationships

| Navigation Property | Target | Type | FK |
|---------------------|--------|------|-----|
| `Project` | `Project` | Many-to-One | `ProjectId` |
| `ProjectAnalysisVersion` | `ProjectAnalysisVersion` | Many-to-One | `ProjectAnalysisVersionId` |
| `WorkDirection` | `IicWorkDirection` | Many-to-One | `WorkDirectionId` |
| `PhysicalVolume` | `PhysicalVolume?` | Many-to-One (optional) | `PhysicalVolumeId` → `PhysicalVolumes.Id` (FK), `Brand` ↔ `DocumentSet` (business match) |

## Database Constraints

- **Primary Key:** `Id`
- **Unique Index:** `{ProjectAnalysisVersionId, ProjectId, DocumentationCode, WorkDirectionId}`
- **Index:** `Brand`, `PhysicalVolumeId`
- **Foreign Key:** `FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId` — `PhysicalVolumeId` → `PhysicalVolumes.Id` with `ON DELETE RESTRICT`

## PhysicalVolume Relationship

The `PhysicalVolumeId` property serves as a standard foreign key to `PhysicalVolumes.Id`, enforced by the database. This connects labor intensity records directly to physical volume definitions.

Additionally, there is a business-level (logical) matching between `PhysicalVolume.DocumentSet` and `ProjectLaborIntensity.Brand`. The `Brand` property ("Марка") corresponds to the physical volume's document set ("Комплект документов"), enabling data reconciliation between labor intensity records and the physical volumes whose documentation packages they belong to. This is not a database-enforced constraint — both columns are indexed for efficient lookups.

The navigation property `PhysicalVolume` is nullable because `PhysicalVolumeId` may be NULL — not all labor intensity records are associated with a specific physical volume.

## Related Documentation

- [Entity Relationships](../entity-relationships.md)
- [PhysicalVolume](./PhysicalVolume.md)
