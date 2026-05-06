# PhysicalVolume

**Type:** Domain Entity (inherits from `DirectoryItem`)
**Location:** `EngineeringPerformance.Infrastructure/Data/Entities/PhysicalVolume.cs`
**Last Updated:** 2026-04-19

## Purpose

Represents a physical volume — a unit of construction, installation, or engineering work (e.g., a building, structure, network segment). Physical volumes form a hierarchical catalog and serve as the foundation for tracking project quantities and labor intensity.

## Properties

| Property | Type | Nullable | Max Length | Description |
|----------|------|----------|------------|-------------|
| `Id` | `int` | No | — | Primary key (inherited from `DirectoryItem`) |
| `ParentId` | `int?` | Yes | — | Parent volume ID (self-referencing hierarchy) |
| `Name` | `string` | No | 500 | Display name |
| `Code` | `string` | No | 50 | EP code — unique identifier for the volume |
| `Description` | `string` | No | 4000 | Detailed description |
| `VolumeTypeId` | `int` | No | — | FK to `PhysicalVolumeType` |
| `ClassifierId` | `int?` | Yes | — | FK to `IicWorkDirection` (optional classifier) |
| `IsAssessment` | `bool?` | Yes | — | Assessment flag |
| `DocumentSet` | `string?` | Yes | 100 | Document package/kit name ("Комплект документов"); business-level match with `ProjectLaborIntensity.Brand` |
| `Coefficient` | `decimal` | No | precision(18,4), default 1 | Multiplier: `ProjectPhysicalVolumes.Quantity × Coefficient` gives volume in **IIC group units** (aligned with norms). Editable in catalog UI. |
| `IsArchived` | `bool` | No | — | Archive flag (inherited from `DirectoryItem`) |

## Relationships

| Navigation Property | Target | Type | FK / Key |
|---------------------|--------|------|----------|
| `Parent` | `PhysicalVolume` | Many-to-One (self) | `ParentId` |
| `Children` | `ICollection<PhysicalVolume>` | One-to-Many (self) | `ParentId` |
| `VolumeType` | `PhysicalVolumeType` | Many-to-One | `VolumeTypeId` |
| `Classifier` | `IicWorkDirection?` | Many-to-One (optional) | `ClassifierId` |
| `ProjectPhysicalVolumes` | `ICollection<ProjectPhysicalVolume>` | One-to-Many | `PhysicalVolumeId` |
| `LaborIntensities` | `ICollection<ProjectLaborIntensity>` | One-to-Many | `PhysicalVolumeId` (FK), `DocumentSet` ↔ `Brand` (business match) |

## Database Constraints

- **Primary Key:** `Id`
- **Indexes:** `Code`, `VolumeTypeId`, `ClassifierId`, `ParentId`, `IsArchived`, `IsAssessment`, `DocumentSet`

## DTO Mapping

Mapped to `PhysicalVolumeDto` which includes all public properties plus `VolumeTypeName` and `ClassifierName` for display purposes.

## Related Documentation

- [Entity Relationships](../entity-relationships.md)
- [ProjectPhysicalVolume](./ProjectPhysicalVolume.md)
- [ProjectLaborIntensity](./ProjectLaborIntensity.md)
