# Plan: Entity Schema & Data Layer Expansion

**Created:** 2026-04-10 15:00
**Orchestration:** orch-2026-04-10-15-00-entity-schema
**Goal:** Fill entity schema gaps, add missing entities, update services/UI, and implement Excel import for Engineering Performance Management
**Total Tasks:** 10
**Priority:** High

## Tasks Overview

1. **Role Entity & User Role FK Migration** → AUTH-001
   - Priority: Critical
   - Dependencies: None
   - Complexity: Moderate
   - Files affected: Core/Models, Core/Enums, Core/DTOs, Core/Services, Infrastructure/Data/Entities, Infrastructure/Data/Configurations, Infrastructure/Services, App/ViewModels, App/Views

2. **Project Entity Field Expansion** → PROJ-001
   - Priority: High
   - Dependencies: None
   - Complexity: Simple
   - Files affected: Core/Models, Core/DTOs, Infrastructure/Data/Entities, Infrastructure/Data/Configurations, Infrastructure/Validators, App/ViewModels, App/Views

3. **PhysicalVolumeType & PhysicalVolume Enhancements** → PV-001
   - Priority: High
   - Dependencies: None
   - Complexity: Simple
   - Files affected: Core/Models, Core/Enums, Core/DTOs, Infrastructure/Data/Entities, Infrastructure/Data/Configurations, Infrastructure/Validators, App/ViewModels, App/Views

4. **UnitOfMeasurement Entity & IicWorkDirection/IicWorkDirectionNorm Updates** → UNIT-001
   - Priority: High
   - Dependencies: None
   - Complexity: Moderate
   - Files affected: Core/Models, Core/DTOs, Core/Services, Infrastructure/Data/Entities, Infrastructure/Data/Configurations, Infrastructure/Services, App/ViewModels, App/Views

5. **ProjectPhysicalVolume & ProjectLaborIntensity Schema Updates** → DATA-001
   - Priority: High
   - Dependencies: PROJ-001
   - Complexity: Moderate
   - Files affected: Core/Models, Core/DTOs, Infrastructure/Data/Entities, Infrastructure/Data/Configurations, Infrastructure/Services

6. **EF Core Migrations** → MIG-001
   - Priority: Critical
   - Dependencies: AUTH-001, PROJ-001, PV-001, UNIT-001, DATA-001
   - Complexity: Moderate
   - Files affected: Infrastructure/Migrations/

7. **ProjectVisibilityService & Service Layer Updates** → SVC-001
   - Priority: High
   - Dependencies: AUTH-001, PROJ-001
   - Complexity: Moderate
   - Files affected: Core/Services, Infrastructure/Services

8. **Excel Import Service** → IMPORT-001
   - Priority: Medium
   - Dependencies: MIG-001
   - Complexity: Complex
   - Files affected: Core/Services, Infrastructure/Services, App/ViewModels, App/Views

9. **Project Dashboard Enhancement** → UI-001
   - Priority: High
   - Dependencies: PROJ-001, SVC-001
   - Complexity: Moderate
   - Files affected: App/ViewModels, App/Views

10. **Database Seeder Updates & Integration Verification** → SEED-001
    - Priority: Medium
    - Dependencies: MIG-001
    - Complexity: Simple
    - Files affected: Infrastructure/DatabaseSeeder.cs, Tests/

## Dependencies Graph

```
AUTH-001 ─┐
PROJ-001 ─┤
PV-001 ───┤                    ┌─── MIG-001 ───┬─── IMPORT-001
UNIT-001 ─┤                    │               │
DATA-001 ─┘ (needs PROJ-001) ──┘               ├─── UI-001 (needs SVC-001)
                                                │
AUTH-001 ─┐                                     └─── SEED-001
PROJ-001 ─┤── SVC-001 ──────────────────────────┘
```

**Critical Path:** AUTH-001/PROJ-001/PV-001/UNIT-001 → DATA-001 → MIG-001 → IMPORT-001 / UI-001 / SEED-001

## Execution Strategy

**Phase 1 — Schema (Parallel):** AUTH-001, PROJ-001, PV-001, UNIT-001 can run in parallel (no inter-dependencies)
**Phase 2 — Data Layer (Sequential):** DATA-001 (depends on PROJ-001)
**Phase 3 — Migration:** MIG-001 (depends on all schema changes)
**Phase 4 — Services + UI (Parallel):** SVC-001, IMPORT-001, UI-001, SEED-001 (after MIG-001)

## Progress (updated by orchestrator)
- ⏳ AUTH-001: Role Entity & User Role FK Migration (Pending)
- ⏳ PROJ-001: Project Entity Field Expansion (Pending)
- ⏳ PV-001: PhysicalVolumeType & PhysicalVolume Enhancements (Pending)
- ⏳ UNIT-001: UnitOfMeasurement Entity & IicWorkDirection/IicWorkDirectionNorm Updates (Pending)
- ⏳ DATA-001: ProjectPhysicalVolume & ProjectLaborIntensity Schema Updates (Pending)
- ⏳ MIG-001: EF Core Migrations (Pending)
- ⏳ SVC-001: ProjectVisibilityService & Service Layer Updates (Pending)
- ⏳ IMPORT-001: Excel Import Service (Pending)
- ⏳ UI-001: Project Dashboard Enhancement (Pending)
- ⏳ SEED-001: Database Seeder Updates & Integration Verification (Pending)

## Architecture Decisions

- **Role FK over enum:** Replace `User.Role` enum with `RoleId` FK to new `Role` entity for extensibility
- **Unit FK over text:** Replace `IicWorkDirectionNorm.Unit` text with FK to `UnitOfMeasurement`
- **ProjectId on data tables:** Add direct `ProjectId` FK to `ProjectPhysicalVolume` and `ProjectLaborIntensity` for easier querying without traversing version
- **Labor intensity split:** Split `LaborHours` into `PlannedHours` and `ActualHours` to match Excel structure
- **Excel import:** Use ClosedXML or EPPlus for reading .xlsx files; create dedicated import service with validation
- **Hierarchical codes:** PhysicalVolume uses string `Code` field (not enum) to support XX-XX-XXX-XX format

## Implementation Notes

### AUTH-001: Role Entity & User Role FK Migration
- Create `Role` entity: `Id`, `Name` (string), `Description` (string?)
- Add `RoleId` FK to `User`, remove `Role` enum property
- Create `RoleConfiguration`: unique index on Name, seed Administrator/ProjectManager/Observer
- Update `UserDto` to include RoleName (via join)
- Update `UserDirectoryService` to handle Role FK
- Update `UserValidator` for RoleId validation
- Update `UsersViewModel` and `UsersWindow` to show role dropdown
- Update `DatabaseSeeder` to create Role rows
- Update `AccessControlService` to use Role FK
- Keep `UserRole` enum temporarily for backward compat in seeders, but mark obsolete

### PROJ-001: Project Entity Field Expansion
- Add to `Project`: `WorkNumber`, `ContractNumber`, `CalendarPlanNumber`, `Type`, `Customer`, `Contractor`, `Gip`, `WorkStatus`, `WorkStart`, `Program` (all string?)
- Update `ProjectDto` with all new fields
- Update `ProjectConfiguration`: max length, nullable columns
- Update `ProjectValidator`: add validation rules for new fields
- Update `ProjectsWindow` XAML: add columns for key fields (Code, Name, GIP, Customer, WorkStatus)
- Update `ProjectsViewModel`: no logic changes needed (generic base handles CRUD)

### PV-001: PhysicalVolumeType & PhysicalVolume Enhancements
- Add `Description` (string?) to `PhysicalVolumeType`
- Add `Code` (string), `Description` (string?) to `PhysicalVolume`
- Update `PhysicalVolumeDto` with Code, Description
- Update `PhysicalVolumeConfiguration`: Code max length, index
- Update `PhysicalVolumeValidator`: Code format validation (optional XX-XX-XXX-XX pattern)
- Update `PhysicalVolumesWindow` XAML: add Code, Description columns
- Update `PhysicalVolumesViewModel`: no logic changes needed

### UNIT-001: UnitOfMeasurement Entity & IicWorkDirection/IicWorkDirectionNorm Updates
- Create `UnitOfMeasurement` entity: `Id`, `Name` (string), `Description` (string?) — inherits `DirectoryItem` but non-hierarchical (ParentId always null)
- Add `Description` (string?) to `IicWorkDirection`
- Add `UnitId` (int?) FK to `IicWorkDirection` → `UnitOfMeasurement`
- Change `IicWorkDirectionNorm.Unit` from string to `UnitId` (int?) FK
- Create `UnitOfMeasurementConfiguration`
- Update `IicWorkDirectionConfiguration`: add Unit FK relationship
- Update `IicWorkDirectionNormConfiguration`: change Unit column to UnitId FK
- Update `IicWorkDirectionDto` with Description, UnitName
- Create `UnitOfMeasurementDto`
- Add `IUnitOfMeasurementService` (IDirectoryService<UnitOfMeasurement>)
- Create `UnitOfMeasurementDirectoryService`
- Update `IicWorkDirectionValidator` for Description, UnitId
- Update `IicWorkDirectionNormValidator` for UnitId FK
- Update `IicWorkDirectionsWindow` XAML: add Description, Unit columns
- Update `IicWorkDirectionNormsWindow` XAML: show unit name instead of text
- Create `UnitOfMeasurementsWindow` and `UnitOfMeasurementsViewModel`
- Register new services in DI

### DATA-001: ProjectPhysicalVolume & ProjectLaborIntensity Schema Updates
- Add `ProjectId` (int) FK to `ProjectPhysicalVolume` with navigation property
- Add `ProjectId` (int) FK to `ProjectLaborIntensity` with navigation property
- Split `ProjectLaborIntensity.LaborHours` → `PlannedHours` (decimal), `ActualHours` (decimal)
- Add `Notes` (string?) to `ProjectLaborIntensity`
- Update `ProjectPhysicalVolumeConfiguration`: ProjectId FK, composite index {ProjectAnalysisVersionId, ProjectId, PhysicalVolumeId}
- Update `ProjectLaborIntensityConfiguration`: ProjectId FK, new columns
- Update DTOs accordingly
- Update services to handle new ProjectId parameter

### MIG-001: EF Core Migrations
- Run `dotnet ef migrations add InitialSchema` from Infrastructure project
- Verify migration includes all entity changes from AUTH-001, PROJ-001, PV-001, UNIT-001, DATA-001
- Ensure seed data for Role and UnitOfMeasurement is included
- Test migration applies cleanly on fresh database

### SVC-001: ProjectVisibilityService & Service Layer Updates
- Update `ProjectVisibilityService.GetAccessibleProjects` to return projects with new fields populated
- Update `ProjectVisibilityService` queries to include Role navigation on User
- Update `IProjectVisibilityService` if needed
- Ensure all generic services work with updated entity schemas
- Wire up validators in DI container (currently not wired)

### IMPORT-001: Excel Import Service
- Create `IExcelImportService` interface in Core/Services
- Create `ExcelImportService` in Infrastructure/Services
- Implement `ImportPhysicalVolumesAsync(string filePath)` — parses 4-level WBS hierarchy with XX-XX-XXX-XX codes
- Implement `ImportLaborIntensityAsync(string filePath)` — parses DocumentationCode, WorkDirectionId, PlannedHours, ActualHours, Notes
- Implement `ImportWorkDirectionNormsAsync(string filePath)` — parses norms with unit matching
- Use ClosedXML for .xlsx reading (add NuGet package to Infrastructure)
- Validate imported data before insertion
- Return import result DTO with success/failure counts and errors
- Create `ExcelImportViewModel` and `ExcelImportWindow` UI
- Wire import commands into relevant directory ViewModels

### UI-001: Project Dashboard Enhancement
- Update `ProjectDashboardViewModel` to load projects with GIP, version count
- Update `ProjectDashboardView` XAML:
  - Grid showing: Code, Name, GIP, Customer, WorkStatus
  - Tooltip on project row showing version count and version names
  - Double-click navigates to project detail
- Use DevExpress Grid with custom tooltip template
- Ensure access control filters visible projects

### SEED-001: Database Seeder Updates & Integration Verification
- Update `DatabaseSeeder.SeedAsync`:
  - Create Role entities (Administrator, ProjectManager, Observer)
  - Create UnitOfMeasurement entities (yard³, tonna, m³, kg, etc.)
  - Update User seeding to use Role FK
  - Update IicWorkDirectionNorm seeding to use Unit FK
  - Add sample Project data with new fields
- Update or create tests for all new entities and services
- Run full test suite to verify no regressions
- Verify application starts cleanly with new schema
