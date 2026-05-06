# Plan: Entity Models and Database Schema Expansion

**Created:** 2026-04-10
**Orchestration:** orch-2026-04-10-00-00-entity-models
**Goal:** Expand database schema with new entities, add missing fields to existing entities, create EF Core migrations, and implement Excel import infrastructure
**Total Tasks:** 10
**Priority:** High

## Context

.NET 10 WPF desktop app using EF Core 10 + SQL Server, CommunityToolkit.Mvvm, DevExpress WPF.
Clean architecture: App → Infrastructure → Core. No migrations exist yet. No Excel import packages.

## Tasks Overview

### Phase 1: Database Schema (Tasks 1-4)

1. **DB-001:** Role Entity + User Role FK Migration
   - Priority: Critical
   - Dependencies: None
   - Complexity: Moderate
   - Files affected: New Role entity, User entity, UserConfiguration, AppDbContext, DatabaseSeeder

2. **DB-002:** Project Entity Field Expansion
   - Priority: High
   - Dependencies: None
   - Complexity: Simple
   - Files affected: Project entity, ProjectConfiguration, ProjectDto

3. **DB-003:** Unit of Measurement Entity + Work Direction Enhancements
   - Priority: High
   - Dependencies: DB-001 (pattern reference)
   - Complexity: Moderate
   - Files affected: New Unit entity, IicWorkDirection, IicWorkDirectionNorm, PhysicalVolume, PhysicalVolumeType

4. **DB-004:** Data Table Records Enhancement + EF Core Migrations
   - Priority: High
   - Dependencies: DB-002, DB-003
   - Complexity: Moderate
   - Files affected: ProjectPhysicalVolume, ProjectLaborIntensity, configurations, AppDbContext, generate migration

### Phase 2: Service Layer (Tasks 5-6)

5. **SVC-001:** Excel Import Infrastructure
   - Priority: High
   - Dependencies: DB-004
   - Complexity: Moderate
   - Files affected: New package (ClosedXML), IExcelImportService, ExcelImportService, DI registration

6. **SVC-002:** Directory Service Updates for New Fields
   - Priority: Medium
   - Dependencies: DB-004
   - Complexity: Simple
   - Files affected: Existing directory services, DTOs, validators

### Phase 3: UI Implementation (Tasks 7-10)

7. **UI-001:** Project Directory Window with New Fields
   - Priority: High
   - Dependencies: DB-002, SVC-002
   - Complexity: Moderate
   - Files affected: ProjectsWindow, ProjectsViewModel, ProjectDto

8. **UI-002:** Physical Volumes Window with Hierarchical View
   - Priority: Medium
   - Dependencies: DB-003, SVC-002
   - Complexity: Moderate
   - Files affected: PhysicalVolumesWindow, PhysicalVolumesViewModel, PhysicalVolumeDto

9. **UI-003:** Work Directions + Norms Windows with Unit Support
   - Priority: Medium
   - Dependencies: DB-003, SVC-002
   - Complexity: Moderate
   - Files affected: IicWorkDirectionsWindow, IicWorkDirectionNormsWindow, ViewModels, DTOs

10. **UI-004:** Project Dashboard Enhancement + Excel Import Form
    - Priority: High
    - Dependencies: UI-001, SVC-001
    - Complexity: Complex
    - Files affected: ProjectDashboardView/ViewModel, ProjectDataUploadForm, ProjectDataUploadFormViewModel

## Dependencies Graph

```
DB-001 ──────────────────────────────────────────────┐
DB-002 ──┐                                           │
DB-003 ──┼── DB-004 ──┬── SVC-001 ──┐               │
         │            │             │               │
         │            └── SVC-002 ──┼── UI-001 ──┐  │
         │                        │             │  │
         │                        ├── UI-002     │  │
         │                        │             │  │
         │                        └── UI-003     │  │
         │                                      │  │
         └──────────────────────────────────────┼──┼── UI-004
                                                │  │
SVC-001 ────────────────────────────────────────┘  │
                                                    │
UI-001 ─────────────────────────────────────────────┘
```

## Execution Strategy

**Sequential execution required** due to dependencies. Recommended order:

1. DB-001 (Role + User FK) - foundation for RBAC
2. DB-002 (Project fields) - independent, simple
3. DB-003 (Unit + enhancements) - independent
4. DB-004 (Data tables + migration) - depends on all entity changes
5. SVC-001 (Excel import) - needs final schema
6. SVC-002 (Service updates) - needs final schema
7. UI-001 (Project UI) - needs services
8. UI-002 (Physical volumes UI) - needs services
9. UI-003 (Work directions UI) - needs services
10. UI-004 (Dashboard + import) - needs everything above

## Architecture Decisions

- **Role entity**: Replace UserRole enum with Role table + FK. Keep enum for backward compatibility during migration.
- **Unit entity**: New lookup table. IicWorkDirectionNorm.Unit changes from string to FK.
- **Excel import**: Use ClosedXML (lightweight, no COM dependencies, good for .xlsx).
- **Project fields**: All new fields nullable with defaults to avoid breaking existing data.
- **PhysicalVolumeType**: Add Description field, keep enum Code for now.
- **PhysicalVolume**: Add Code field (WBS format XX-XX-XXX-XX), Description field.
- **IicWorkDirection**: Add Description, UnitOfMeasurementId FK.
- **Data tables**: Add ProjectId FK to both ProjectPhysicalVolume and ProjectLaborIntensity for direct project access.

## Implementation Notes

- All new entities inherit from appropriate base class (DirectoryItem for lookups, RegisterEntry for registers)
- Use readonly properties where possible
- EF Core configurations in separate files under Data/Configurations/
- DTOs in Core/DTOs/ with AutoMapper-compatible structure
- Validators in Infrastructure/Validators/
- Services follow existing patterns (IDirectoryService<T>, IRegisterService<T>)
- Excel import service returns Result<T> pattern for error handling
- All services registered in App.xaml.cs DI container

## Progress (updated by orchestrator)
- ⏳ DB-001: Role Entity + User Role FK Migration (Pending)
- ⏳ DB-002: Project Entity Field Expansion (Pending)
- ⏳ DB-003: Unit of Measurement Entity + Work Direction Enhancements (Pending)
- ⏳ DB-004: Data Table Records Enhancement + EF Core Migrations (Pending)
- ⏳ SVC-001: Excel Import Infrastructure (Pending)
- ⏳ SVC-002: Directory Service Updates for New Fields (Pending)
- ⏳ UI-001: Project Directory Window with New Fields (Pending)
- ⏳ UI-002: Physical Volumes Window with Hierarchical View (Pending)
- ⏳ UI-003: Work Directions + Norms Windows with Unit Support (Pending)
- ⏳ UI-004: Project Dashboard Enhancement + Excel Import Form (Pending)
