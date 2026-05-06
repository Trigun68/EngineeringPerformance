# ProjectPhysicalVolume

**Type:** Data Entity (inherits from `DataTableRecord`)
**Location:** `EngineeringPerformance.Infrastructure/Data/Entities/ProjectPhysicalVolume.cs`
**Last Updated:** 2026-04-15

## Purpose

Represents project-specific physical volume data — the quantities, facility details, and metadata associated with a physical volume within a specific project and analysis version.

## Properties

| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| `Id` | `int` | No | — | Primary key (inherited from `DataTableRecord`) |
| `ProjectAnalysisVersionId` | `int` | No | — | FK to `ProjectAnalysisVersion` (inherited) |
| `ProjectId` | `int` | No | — | FK to `Project` |
| `PhysicalVolumeId` | `int` | No | — | FK to `PhysicalVolume` |
| `Quantity` | `decimal` | No | precision(18,4) | Quantity in **catalog / document** units for the linked `PhysicalVolume`. For analytics and norms in IIC units: effective volume = `Quantity × PhysicalVolume.Coefficient`. Planning UI shows IIC units; save path divides by `Coefficient` before storing `Quantity`. |
| `FacilityCode` | `string?` | Yes | 100 | Facility code |
| `FacilityName` | `string?` | Yes | 500 | Facility name |
| `BindingType` | `string?` | Yes | 200 | Binding type |
| `DocumentSet` | `string?` | Yes | 100 | Document package name |
| `CatalogId` | `string?` | Yes | 100 | Catalog ID — matches `PhysicalVolume.Code` for business-level data matching |
| `Tag` | `string?` | Yes | 100 | Tag identifier |
| `WorkComment` | `string?` | Yes | 500 | Work comment |
| `EstimationBasis` | `string?` | Yes | 500 | Estimation basis |
| `LaborHoursPerUnit` | `decimal?` | Yes | precision(18,4) | Labor hours per unit |
| `MachineHoursPerUnit` | `decimal?` | Yes | precision(18,4) | Machine hours per unit |
| `TotalMachineHours` | `decimal?` | Yes | precision(18,4) | Total machine hours |
| `Status` | `string?` | Yes | 50 | Status |
| `CreatedAt` | `DateTime` | No | — | Creation timestamp (inherited) |
| `ModifiedAt` | `DateTime?` | Yes | — | Last modification timestamp (inherited) |

## Relationships

| Navigation Property | Target | Type | FK |
|---------------------|--------|------|-----|
| `Project` | `Project` | Many-to-One | `ProjectId` |
| `ProjectAnalysisVersion` | `ProjectAnalysisVersion` | Many-to-One | `ProjectAnalysisVersionId` |
| `PhysicalVolume` | `PhysicalVolume` | Many-to-One | `PhysicalVolumeId` |

## Database Constraints

- **Primary Key:** `Id`
- **Unique Index:** `{ProjectAnalysisVersionId, ProjectId, PhysicalVolumeId}`
- **Index:** `CatalogId`

## Business-Level Matching

The `CatalogId` property is designed to match `PhysicalVolume.Code` for business-level data reconciliation. While the FK `PhysicalVolumeId` enforces referential integrity, the `CatalogId` ↔ `Code` match enables cross-referencing between project data and the canonical volume catalog.

## Related Documentation

- [Entity Relationships](../entity-relationships.md)
- [PhysicalVolume](./PhysicalVolume.md)
