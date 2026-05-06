# Plan: PhysicalVolume Relationships

**Created:** 2026-04-15 22:03
**Orchestration:** orch-2026-04-15-physical-volume-relations
**Goal:** Establish two relationships: PhysicalVolume ↔ ProjectPhysicalVolume (via Code→CatalogId) and PhysicalVolume ↔ ProjectLaborIntensity (via DocumentSet→Brand)
**Total Tasks:** 7
**Priority:** High

## Tasks Overview

1. **DB-001:** Add DocumentSet property to PhysicalVolume entity
   - Priority: High
   - Dependencies: None
   - Files: `PhysicalVolume.cs`

2. **DB-002:** Add navigation properties to PhysicalVolume entity
   - Priority: High
   - Dependencies: DB-001
   - Files: `PhysicalVolume.cs`

3. **DB-003:** Add navigation properties to ProjectLaborIntensity entity
   - Priority: High
   - Dependencies: DB-001
   - Files: `ProjectLaborIntensity.cs`

4. **DB-004:** Configure EF Core relationships in entity configurations
   - Priority: High
   - Dependencies: DB-002, DB-003
   - Files: `PhysicalVolumeConfiguration.cs`, `ProjectLaborIntensityConfiguration.cs`

5. **DB-005:** Create and apply EF Core migration
   - Priority: High
   - Dependencies: DB-004
   - Files: New migration file under `Migrations/`

6. **DTO-001:** Update PhysicalVolumeDto to include DocumentSet
   - Priority: Medium
   - Dependencies: DB-001
   - Files: `PhysicalVolumeDto.cs`, mapping services

7. **DOC-001:** Update documentation
   - Priority: Low
   - Dependencies: DB-005, DTO-001
   - Files: Project documentation

## Dependencies Graph

```
DB-001 → DB-002 → DB-004 → DB-005 → DOC-001
DB-001 → DB-003 → DB-004
DB-001 → DTO-001 → DOC-001
```

## Progress (updated by orchestrator)
- ⏳ DB-001: Add DocumentSet property to PhysicalVolume entity (Pending)
- ⏳ DB-002: Add navigation properties to PhysicalVolume entity (Pending)
- ⏳ DB-003: Add navigation properties to ProjectLaborIntensity entity (Pending)
- ⏳ DB-004: Configure EF Core relationships in entity configurations (Pending)
- ⏳ DB-005: Create and apply EF Core migration (Pending)
- ⏳ DTO-001: Update PhysicalVolumeDto to include DocumentSet (Pending)
- ⏳ DOC-001: Update documentation (Pending)

## Architecture Decisions

### Relationship 1: PhysicalVolume ↔ ProjectPhysicalVolume
- **Join condition:** `PhysicalVolume.Code` → `ProjectPhysicalVolume.CatalogId`
- **Type:** One-to-many (one PhysicalVolume can have many ProjectPhysicalVolume records)
- **Implementation:** Add navigation property `ICollection<ProjectPhysicalVolume> ProjectPhysicalVolumes` to PhysicalVolume
- **Note:** The FK `PhysicalVolumeId` already exists on ProjectPhysicalVolume. The Code→CatalogId matching is a **logical/business-level** relationship, not a database FK. The existing FK (PhysicalVolumeId) is the actual database relationship. The Code→CatalogId link is used for data matching/import purposes.

### Relationship 2: PhysicalVolume ↔ ProjectLaborIntensity
- **Join condition:** `PhysicalVolume.DocumentSet` → `ProjectLaborIntensity.Brand`
- **Type:** One-to-many (one PhysicalVolume can have many ProjectLaborIntensity records)
- **Implementation:** 
  - Add `DocumentSet` property (string, max 100) to PhysicalVolume
  - Add navigation property `ICollection<ProjectLaborIntensity> LaborIntensities` to PhysicalVolume
  - Add navigation property `PhysicalVolume? PhysicalVolume` to ProjectLaborIntensity
- **Note:** This is also a **logical/business-level** relationship matched by string values, not a database FK. No FK column will be added. The relationship is configured using `HasPrincipalKey`/`HasForeignKey` on the string properties for EF Core queries.

### EF Core Configuration Approach
- Both relationships use string-based matching (Code→CatalogId, DocumentSet→Brand) rather than traditional integer FKs
- For EF Core, configure these as relationships using `.HasPrincipalKey()` and `.HasForeignKey()` on the string properties
- Mark relationships as optional (nullable) since not all records may have matching values
- Add appropriate indexes on the string columns for query performance

## Implementation Notes

### Task DB-001: Add DocumentSet to PhysicalVolume
```csharp
// In PhysicalVolume.cs, add:
public string? DocumentSet { get; set; }
```
- Nullable string, max length 100 (to match ProjectLaborIntensity.Brand)
- Add to PhysicalVolumeConfiguration: `.Property(pv => pv.DocumentSet).HasMaxLength(100)`
- Add index: `builder.HasIndex(pv => pv.DocumentSet)`

### Task DB-002: Navigation properties on PhysicalVolume
```csharp
// In PhysicalVolume.cs, add:
public ICollection<ProjectPhysicalVolume> ProjectPhysicalVolumes { get; set; } = [];
public ICollection<ProjectLaborIntensity> LaborIntensities { get; set; } = [];
```

### Task DB-003: Navigation property on ProjectLaborIntensity
```csharp
// In ProjectLaborIntensity.cs, add:
public PhysicalVolume? PhysicalVolume { get; set; }
```

### Task DB-004: EF Core Configuration
In `PhysicalVolumeConfiguration.cs`:
```csharp
// Relationship 1: PhysicalVolume → ProjectPhysicalVolumes (via Code → CatalogId)
builder.HasMany(pv => pv.ProjectPhysicalVolumes)
    .WithOne(pv => pv.PhysicalVolume)
    .HasPrincipalKey(pv => pv.Code)
    .HasForeignKey(pv => pv.CatalogId)
    .OnDelete(DeleteBehavior.Restrict);

// Relationship 2: PhysicalVolume → LaborIntensities (via DocumentSet → Brand)
builder.HasMany(pv => pv.LaborIntensities)
    .WithOne(li => li.PhysicalVolume)
    .HasPrincipalKey(pv => pv.DocumentSet)
    .HasForeignKey(li => li.Brand)
    .OnDelete(DeleteBehavior.Restrict);
```

In `ProjectLaborIntensityConfiguration.cs`:
```csharp
// Configure the relationship with PhysicalVolume (via Brand → DocumentSet)
builder.HasOne(li => li.PhysicalVolume)
    .WithMany(pv => pv.LaborIntensities)
    .HasPrincipalKey(pv => pv.DocumentSet)
    .HasForeignKey(li => li.Brand)
    .OnDelete(DeleteBehavior.Restrict);
```

### Task DB-005: Migration
- Run `dotnet ef migrations add AddPhysicalVolumeRelationships`
- Verify migration adds:
  - `DocumentSet` column to `PhysicalVolumes` table
  - Index on `PhysicalVolumes.DocumentSet`
  - No new FK columns (relationships use existing string columns)
- Run `dotnet ef database update`

### Task DTO-001: Update DTO
- Add `DocumentSet` parameter to `PhysicalVolumeDto` record
- Update any mapping/conversion code that creates PhysicalVolumeDto from PhysicalVolume entity
- Check `PhysicalVolumeDirectoryService.cs` for DTO mapping

### Task DOC-001: Documentation
- Update any entity relationship diagrams or documentation
- Document the new relationships and their business meaning

## Risks and Edge Cases

1. **Data integrity:** String-based relationships (Code→CatalogId, DocumentSet→Brand) are not enforced at the database level as FKs. Data mismatches could occur if values don't align exactly (case sensitivity, trailing spaces).
2. **Existing data:** The migration adds a nullable `DocumentSet` column. Existing PhysicalVolume records will have NULL values. A data seeding script may be needed to populate DocumentSet from existing data sources.
3. **Performance:** String-based joins are slower than integer FK joins. Indexes on `Code`, `CatalogId`, `DocumentSet`, and `Brand` are critical.
4. **Uniqueness:** `PhysicalVolume.Code` should be unique for the Code→CatalogId relationship to work correctly. Verify there's a unique constraint or index.
