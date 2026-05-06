# Entity Relationships — PhysicalVolume

**Last Updated:** 2026-04-15

## Overview

The `PhysicalVolume` entity is a central domain model representing physical volumes (construction works, installations, etc.) in the Engineering Performance system. It participates in several relationships that connect project data, labor intensity metrics, and hierarchical structure.

## Entity Diagram

```
PhysicalVolume
├── Parent (self-ref) ──────────────┐
│                                   ▼
├── Children (self-ref) ◄───────────┘
│
├── VolumeType ───────────────────► PhysicalVolumeType
│
├── Classifier ───────────────────► IicWorkDirection
│
├── ProjectPhysicalVolumes ────────┐
│                                  ▼
│                          ProjectPhysicalVolume
│                          ├── Project
│                          ├── ProjectAnalysisVersion
│                          └── PhysicalVolume (FK: PhysicalVolumeId)
│                              Business match: Code ↔ CatalogId
│
└── LaborIntensities ────────────┐
                                 ▼
                          ProjectLaborIntensity
                          ├── Project
                          ├── ProjectAnalysisVersion
                          ├── WorkDirection
                          └── PhysicalVolume (FK: PhysicalVolumeId → PhysicalVolumes.Id)
                              Business match: DocumentSet ↔ Brand
```

## Relationships

### 1. Self-Referencing Hierarchy (Parent/Children)

| Aspect | Detail |
|--------|--------|
| **Type** | One-to-Many (self-referencing) |
| **Principal** | `PhysicalVolume` (Parent) |
| **Dependent** | `PhysicalVolume` (Children) |
| **FK Column** | `ParentId` |
| **Delete Behavior** | `Restrict` |
| **Navigation** | `PhysicalVolume.Parent`, `PhysicalVolume.Children` |

Physical volumes form a tree structure. Root volumes have `ParentId = NULL`. Child volumes inherit context from their parent (e.g., a building contains structures, which contain networks).

---

### 2. PhysicalVolume → PhysicalVolumeType

| Aspect | Detail |
|--------|--------|
| **Type** | Many-to-One |
| **Principal** | `PhysicalVolumeType` |
| **Dependent** | `PhysicalVolume` |
| **FK Column** | `VolumeTypeId` |
| **Delete Behavior** | `Restrict` |
| **Navigation** | `PhysicalVolume.VolumeType` |

Each physical volume is classified by a type (e.g., СМР, ПНР, ПИР). The type is required and cannot be deleted while volumes reference it.

---

### 3. PhysicalVolume → IicWorkDirection (Classifier)

| Aspect | Detail |
|--------|--------|
| **Type** | Many-to-One (optional) |
| **Principal** | `IicWorkDirection` |
| **Dependent** | `PhysicalVolume` |
| **FK Column** | `ClassifierId` |
| **Delete Behavior** | `SetNull` |
| **Navigation** | `PhysicalVolume.Classifier` |

An optional classifier linking a physical volume to a work direction. If the work direction is deleted, the classifier reference is set to NULL.

---

### 4. PhysicalVolume ↔ ProjectPhysicalVolume (Code ↔ CatalogId)

| Aspect | Detail |
|--------|--------|
| **Type** | One-to-Many |
| **Principal** | `PhysicalVolume` |
| **Dependent** | `ProjectPhysicalVolume` |
| **FK Column** | `PhysicalVolumeId` |
| **Delete Behavior** | `Restrict` |
| **Navigation** | `PhysicalVolume.ProjectPhysicalVolumes`, `ProjectPhysicalVolume.PhysicalVolume` |
| **Business Match** | `PhysicalVolume.Code` ↔ `ProjectPhysicalVolume.CatalogId` |

**Database FK:** The standard EF Core foreign key `PhysicalVolumeId` enforces referential integrity.

**Business-level relationship:** Beyond the FK, there is a logical/business-level matching between `PhysicalVolume.Code` (the "EP Код") and `ProjectPhysicalVolume.CatalogId` (the "ID по каталогу"). This is used for data matching and reconciliation — the catalog ID in project data should correspond to the physical volume's code. Both columns are indexed for efficient lookups.

**Business meaning:** When project data is imported, each project physical volume record references a master physical volume definition. The `CatalogId` in the project record should match the `Code` of the corresponding master volume, enabling cross-referencing between project-specific quantities and the canonical volume catalog.

---

### 5. PhysicalVolume ↔ ProjectLaborIntensity (PhysicalVolumeId FK, DocumentSet ↔ Brand)

| Aspect | Detail |
|--------|--------|
| **Type** | One-to-Many |
| **Principal** | `PhysicalVolume` |
| **Dependent** | `ProjectLaborIntensity` |
| **FK Column** | `ProjectLaborIntensity.PhysicalVolumeId` |
| **Delete Behavior** | `Restrict` |
| **Navigation** | `PhysicalVolume.LaborIntensities`, `ProjectLaborIntensity.PhysicalVolume` (nullable) |
| **Business Match** | `PhysicalVolume.DocumentSet` ↔ `ProjectLaborIntensity.Brand` |

**Database FK:** The standard EF Core foreign key `PhysicalVolumeId` enforces referential integrity: `ProjectLaborIntensities.PhysicalVolumeId` → `PhysicalVolumes.Id`.

**Business-level relationship:** Beyond the FK, there is a logical/business-level matching between `PhysicalVolume.DocumentSet` (the "Комплект документов") and `ProjectLaborIntensity.Brand` (the "Марка"). This is used for data matching and reconciliation — the brand in labor intensity records should correspond to the physical volume's document set. Both columns are indexed for efficient lookups.

**Business meaning:** When project data is imported, each project labor intensity record references a master physical volume definition via `PhysicalVolumeId`. The `Brand` in the labor intensity record should match the `DocumentSet` of the corresponding master volume, enabling cross-referencing between labor effort data and the physical volumes whose documentation packages they belong to. A labor intensity record may have a NULL `Brand`, making the navigation property nullable.

---

## Database Schema Changes

### Migration: `20260415193900_AddPhysicalVolumeRelationships`

| Operation | Table | Column/Constraint | Details |
|-----------|-------|-------------------|---------|
| Add column | `ProjectLaborIntensities` | `PhysicalVolumeId` | `int`, NULLABLE |
| Add column | `PhysicalVolumes` | `DocumentSet` | `nvarchar(100)`, NULLABLE |
| Add index | `ProjectPhysicalVolumes` | `IX_ProjectPhysicalVolumes_CatalogId` | Index on `CatalogId` |
| Add index | `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_Brand` | Index on `Brand` |
| Add index | `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_PhysicalVolumeId` | Index on `PhysicalVolumeId` |
| Add index | `PhysicalVolumes` | `IX_PhysicalVolumes_DocumentSet` | Index on `DocumentSet` |
| Add FK | `ProjectLaborIntensities` | `FK_..._PhysicalVolumeId` | `PhysicalVolumeId` → `PhysicalVolumes.Id` |

### Indexes Summary

| Table | Index | Column(s) | Purpose |
|-------|-------|-----------|---------|
| `PhysicalVolumes` | `IX_PhysicalVolumes_DocumentSet` | `DocumentSet` | Business matching (DocumentSet ↔ Brand) |
| `ProjectPhysicalVolumes` | `IX_ProjectPhysicalVolumes_CatalogId` | `CatalogId` | Business matching (Code ↔ CatalogId) |
| `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_PhysicalVolumeId` | `PhysicalVolumeId` | FK support + query performance |
| `ProjectLaborIntensities` | `IX_ProjectLaborIntensities_Brand` | `Brand` | Business matching (DocumentSet ↔ Brand) |

---

## Related Files

| File | Path |
|------|------|
| Entity | `EngineeringPerformance.Infrastructure/Data/Entities/PhysicalVolume.cs` |
| Entity | `EngineeringPerformance.Infrastructure/Data/Entities/ProjectPhysicalVolume.cs` |
| Entity | `EngineeringPerformance.Infrastructure/Data/Entities/ProjectLaborIntensity.cs` |
| Configuration | `EngineeringPerformance.Infrastructure/Data/Configurations/PhysicalVolumeConfiguration.cs` |
| Configuration | `EngineeringPerformance.Infrastructure/Data/Configurations/ProjectPhysicalVolumeConfiguration.cs` |
| Configuration | `EngineeringPerformance.Infrastructure/Data/Configurations/ProjectLaborIntensityConfiguration.cs` |
| DTO | `EngineeringPerformance.Core/DTOs/PhysicalVolumeDto.cs` |
| Migration | `EngineeringPerformance.Infrastructure/Migrations/20260415193900_AddPhysicalVolumeRelationships.cs` |
