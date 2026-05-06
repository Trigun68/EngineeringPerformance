# Plan: C# .NET 8 WPF Enterprise Application

**Created:** 2026-04-10 15:30
**Orchestration:** orch-2026-04-10-15-30-wpf-enterprise
**Goal:** Build enterprise WPF application with authentication, access control, directories, registers, data tables, and forms
**Total Tasks:** 10
**Priority:** High

## Tasks Overview

### TASK-001: Project Scaffolding & Solution Structure
- **Priority:** Critical
- **Dependencies:** None
- **Complexity:** Moderate
- **Files to create:**
  - `IICPerformance.sln` — solution file
  - `src/IICPerformance.App/IICPerformance.App.csproj` — WPF application entry point
  - `src/IICPerformance.Domain/IICPerformance.Domain.csproj` — domain models, value objects, enums
  - `src/IICPerformance.Application/IICPerformance.Application.csproj` — use cases, DTOs, services interfaces
  - `src/IICPerformance.Infrastructure/IICPerformance.Infrastructure.csproj` — EF Core, repositories, external services
  - `src/IICPerformance.UI/IICPerformance.UI.csproj` — ViewModels, Views, converters, behaviors
  - `tests/IICPerformance.Tests/IICPerformance.Tests.csproj` — unit/integration tests
  - `Directory.Build.props` — shared build settings, nullable enabled, C# 12
  - NuGet packages: CommunityToolkit.Mvvm, DevExpress.WPF, Serilog, Serilog.Sinks.File, Serilog.Sinks.Console, FluentValidation, AutoMapper, Microsoft.EntityFrameworkCore.SqlServer, Microsoft.EntityFrameworkCore.Tools
- **Acceptance criteria:**
  - Solution builds successfully with `dotnet build`
  - All projects reference each other correctly (clean architecture dependency flow)
  - Nullable reference types enabled globally
  - Serilog configured with console + file sinks in App.xaml.cs
  - DI container registered in App.xaml.cs (Microsoft.Extensions.DependencyInjection)

### TASK-002: Domain Models & Value Objects
- **Priority:** Critical
- **Dependencies:** TASK-001
- **Complexity:** Moderate
- **Files to create:**
  - `src/IICPerformance.Domain/Enums/UserRole.cs` — Administrator, ProjectManager, ProjectObserver
  - `src/IICPerformance.Domain/Enums/AuthenticationMode.cs` — Windows, SqlServer
  - `src/IICPerformance.Domain/Enums/ProjectVisibility.cs` — AllUsers, SpecificUsers
  - `src/IICPerformance.Domain/Enums/PermissionLevel.cs` — None, Read, Edit
  - `src/IICPerformance.Domain/ValueObjects/UserId.cs` — fluent wrapper
  - `src/IICPerformance.Domain/ValueObjects/ProjectId.cs` — fluent wrapper
  - `src/IICPerformance.Domain/ValueObjects/Code.cs` — domain primitive for codes
  - `src/IICPerformance.Domain/ValueObjects/Name.cs` — domain primitive for names
  - `src/IICPerformance.Domain/Entities/User.cs` — Id, WindowsLogin, Username, RoleId, IsActive, IsArchived
  - `src/IICPerformance.Domain/Entities/Role.cs` — Id, Name, Description
  - `src/IICPerformance.Domain/Entities/Project.cs` — Id, Code, Name, Visibility, IsArchived
  - `src/IICPerformance.Domain/Entities/ProjectAnalysisVersion.cs` — Id, ProjectId, VersionName, CreatedDate, IsArchived
  - `src/IICPerformance.Domain/Entities/PhysicalVolumeType.cs` — Id, Code, Name, IsArchived
  - `src/IICPerformance.Domain/Entities/PhysicalVolume.cs` — Id, VolumeTypeId, IicDirectionId, Value, IsArchived
  - `src/IICPerformance.Domain/Entities/IicWorkDirectionClassifier.cs` — Id, Code, Name, ParentId (hierarchical), IsArchived
  - `src/IICPerformance.Domain/Entities/ProjectUser.cs` — ProjectId, UserId (many-to-many for visibility)
  - `src/IICPerformance.Domain/Entities/AccessRight.cs` — Id, RoleId, ObjectType, CanRead, CanEdit
  - `src/IICPerformance.Domain/Entities/IicWorkDirectionNorm.cs` — Id, DirectionId, NormValue, IsArchived
  - `src/IICPerformance.Domain/Entities/PhysicalVolumeByProject.cs` — Id, ProjectAnalysisVersionId, VolumeId, Value
  - `src/IICPerformance.Domain/Entities/LaborIntensityByProject.cs` — Id, ProjectAnalysisVersionId, DocumentationCodeId, Value
  - `src/IICPerformance.Domain/Exceptions/DomainException.cs` — base domain exception
  - `src/IICPerformance.Domain/Exceptions/InvalidCodeException.cs`
  - `src/IICPerformance.Domain/Exceptions/InvalidNameException.cs`
- **Acceptance criteria:**
  - All entities have proper navigation properties
  - Value objects are immutable with factory methods
  - Hierarchical entities have ParentId self-reference
  - Custom exceptions inherit from DomainException
  - All domain classes are sealed or abstract as appropriate

### TASK-003: Database Layer (EF Core)
- **Priority:** Critical
- **Dependencies:** TASK-002
- **Complexity:** Complex
- **Files to create:**
  - `src/IICPerformance.Infrastructure/Data/IICPerformanceDbContext.cs` — DbContext with all DbSets
  - `src/IICPerformance.Infrastructure/Data/Configurations/UserConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/RoleConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/ProjectConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/AccessRightConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/PhysicalVolumeTypeConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/IicWorkDirectionClassifierConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/ProjectAnalysisVersionConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/PhysicalVolumeConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/IicWorkDirectionNormConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/PhysicalVolumeByProjectConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/LaborIntensityByProjectConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Configurations/ProjectUserConfiguration.cs`
  - `src/IICPerformance.Infrastructure/Data/Migrations/` — initial migration
  - `src/IICPerformance.Infrastructure/Data/SeedData.cs` — seed roles, default admin user, access rights
  - `src/IICPerformance.Infrastructure/Repositories/BaseRepository.cs` — generic repository with async CRUD
  - `src/IICPerformance.Infrastructure/Repositories/IRepository.cs` — generic repository interface
- **Acceptance criteria:**
  - EF Core migrations generate correctly
  - Database update creates all tables with proper relationships
  - Seed data inserts 3 roles, 1 admin user, baseline access rights
  - Soft-delete (IsArchived) pattern applied to all entities
  - Connection string configurable via appsettings.json

### TASK-004: Application Services & DTOs
- **Priority:** High
- **Dependencies:** TASK-002, TASK-003
- **Complexity:** Moderate
- **Files to create:**
  - `src/IICPerformance.Application/DTOs/` — DTOs for all entities (User, Project, PhysicalVolumeType, etc.)
  - `src/IICPerformance.Application/DTOs/DbConnectionSettings.cs` — Server, Database, AuthMode, Username, Password
  - `src/IICPerformance.Application/Services/Interfaces/IUserService.cs`
  - `src/IICPerformance.Application/Services/Interfaces/IAuthService.cs`
  - `src/IICPerformance.Application/Services/Interfaces/IAccessControlService.cs`
  - `src/IICPerformance.Application/Services/Interfaces/IProjectService.cs`
  - `src/IICPerformance.Application/Services/Interfaces/IDirectoryService.cs`
  - `src/IICPerformance.Application/Services/Interfaces/IDbConnectionService.cs`
  - `src/IICPerformance.Application/Services/` — implementations for all interfaces
  - `src/IICPerformance.Application/Mapping/MappingProfile.cs` — AutoMapper profiles
  - `src/IICPerformance.Application/Validators/` — FluentValidation for User, Project, PhysicalVolumeType
  - `src/IICPerformance.Application/Exceptions/AuthenticationFailedException.cs`
  - `src/IICPerformance.Application/Exceptions/AccessDeniedException.cs`
  - `src/IICPerformance.Application/Exceptions/DbConnectionFailedException.cs`
- **Acceptance criteria:**
  - All DTOs map correctly from domain entities via AutoMapper
  - Validators enforce business rules
  - AuthService uses WindowsIdentity for Windows auth
  - AccessControlService checks CanRead/CanEdit per user role + object type
  - DbConnectionService persists/loads connection settings (encrypted)
  - Service interfaces are framework-agnostic

### TASK-005: Authentication System & Login Dialog
- **Priority:** High
- **Dependencies:** TASK-004
- **Complexity:** Moderate
- **Files to create:**
  - `src/IICPerformance.UI/ViewModels/LoginViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/DbConnectionSettingsViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/MainViewModel.cs`
  - `src/IICPerformance.UI/Views/LoginWindow.xaml` — username, password, login button, gear icon
  - `src/IICPerformance.UI/Views/LoginWindow.xaml.cs`
  - `src/IICPerformance.UI/Views/DbConnectionSettingsDialog.xaml` — server, database, auth mode, credentials
  - `src/IICPerformance.UI/Views/DbConnectionSettingsDialog.xaml.cs`
  - `src/IICPerformance.UI/Views/MainWindow.xaml` — placeholder shell
  - `src/IICPerformance.UI/Views/MainWindow.xaml.cs`
  - `src/IICPerformance.App/App.xaml.cs` — startup logic: Windows auth -> login if fails
  - `src/IICPerformance.App/Services/NavigationService.cs`
  - `src/IICPerformance.App/Services/SessionService.cs`
- **Acceptance criteria:**
  - On startup: auto-authenticate via Windows identity against Users table
  - If Windows user found and DB connected -> skip login, open MainWindow
  - If not found or DB fails -> show LoginWindow
  - Login dialog validates credentials against Users table
  - Gear button opens DbConnectionSettingsDialog
  - Connection settings test button validates DB connectivity
  - Settings persisted between sessions (encrypted)
  - SessionService holds CurrentUser throughout app lifetime

### TASK-006: Access Control System & Base Infrastructure
- **Priority:** High
- **Dependencies:** TASK-004, TASK-005
- **Complexity:** Moderate
- **Files to create:**
  - `src/IICPerformance.UI/ViewModels/Base/ViewModelBase.cs`
  - `src/IICPerformance.UI/ViewModels/Base/DirectoryViewModelBase.cs` — CRUD, archive toggle, hierarchical support
  - `src/IICPerformance.UI/ViewModels/Base/RegisterViewModelBase.cs`
  - `src/IICPerformance.UI/ViewModels/Base/DataTableViewModelBase.cs`
  - `src/IICPerformance.UI/ViewModels/Base/FormViewModelBase.cs`
  - `src/IICPerformance.UI/ViewModels/Base/INavigationAware.cs`
  - `src/IICPerformance.UI/Services/AccessControlManager.cs`
  - `src/IICPerformance.UI/Behaviors/AccessControlBehavior.cs`
  - `src/IICPerformance.UI/Converters/VisibilityConverter.cs`
  - `src/IICPerformance.UI/Converters/PermissionToVisibilityConverter.cs`
  - `src/IICPerformance.Domain/Enums/ObjectType.cs` — all application objects
- **Acceptance criteria:**
  - DirectoryViewModelBase provides: AddCommand, EditCommand, DeleteCommand, ArchiveCommand, UnarchiveCommand, ShowArchived toggle
  - Hierarchical directories use TreeView-compatible data structures
  - AccessControlManager: CanRead(ObjectType), CanEdit(ObjectType) based on current user role
  - Permission-based visibility: objects without read permission hidden from navigation
  - All base classes use composition, not deep inheritance

### TASK-007: Directory CRUD Windows (6 directories)
- **Priority:** High
- **Dependencies:** TASK-006
- **Complexity:** Complex
- **Files to create:**
  - `src/IICPerformance.UI/ViewModels/Directories/UsersViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Directories/PhysicalVolumeTypesViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Directories/PhysicalVolumesViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Directories/ProjectsViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Directories/ProjectAnalysisVersionsViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Directories/IicWorkDirectionClassifiersViewModel.cs`
  - `src/IICPerformance.UI/Views/Directories/UsersWindow.xaml`
  - `src/IICPerformance.UI/Views/Directories/PhysicalVolumeTypesWindow.xaml`
  - `src/IICPerformance.UI/Views/Directories/PhysicalVolumesWindow.xaml`
  - `src/IICPerformance.UI/Views/Directories/ProjectsWindow.xaml`
  - `src/IICPerformance.UI/Views/Directories/ProjectAnalysisVersionsWindow.xaml`
  - `src/IICPerformance.UI/Views/Directories/IicWorkDirectionClassifiersWindow.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/UserEditDialog.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/ProjectEditDialog.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/PhysicalVolumeTypeEditDialog.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/PhysicalVolumeEditDialog.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/ProjectAnalysisVersionEditDialog.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/IicWorkDirectionClassifierEditDialog.xaml`
- **Acceptance criteria:**
  - All 6 directory windows inherit from DirectoryViewModelBase
  - Each window: DevExpress Grid with Add/Edit/Delete/Archive/Unarchive toolbar buttons
  - ShowArchived toggle button visible in all windows
  - Hierarchical directories use DevExpress TreeList
  - Edit dialogs validate input via FluentValidation
  - Archive hides items from default view, Unarchive restores
  - Placeholder designs — functional but not polished

### TASK-008: Register & Data Table Windows
- **Priority:** Medium
- **Dependencies:** TASK-006
- **Complexity:** Moderate
- **Files to create:**
  - `src/IICPerformance.UI/ViewModels/Registers/IicWorkDirectionNormsViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/DataTables/PhysicalVolumesByProjectViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/DataTables/LaborIntensityByProjectViewModel.cs`
  - `src/IICPerformance.UI/Views/Registers/IicWorkDirectionNormsWindow.xaml`
  - `src/IICPerformance.UI/Views/DataTables/PhysicalVolumesByProjectWindow.xaml`
  - `src/IICPerformance.UI/Views/DataTables/LaborIntensityByProjectWindow.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/IicWorkDirectionNormEditDialog.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/PhysicalVolumeByProjectEditDialog.xaml`
  - `src/IICPerformance.UI/Views/Dialogs/LaborIntensityByProjectEditDialog.xaml`
- **Acceptance criteria:**
  - Register window inherits from RegisterViewModelBase
  - Data table windows inherit from DataTableViewModelBase
  - PhysicalVolumesByProject linked to ProjectAnalysisVersion (dropdown selector)
  - LaborIntensityByProject linked to ProjectAnalysisVersion + documentation code
  - DevExpress Grid with CRUD operations
  - Placeholder designs — functional but not polished

### TASK-009: Main Window with Navigation & Dashboard
- **Priority:** High
- **Dependencies:** TASK-005, TASK-006
- **Complexity:** Complex
- **Files to create:**
  - `src/IICPerformance.UI/ViewModels/Navigation/NavigationItem.cs`
  - `src/IICPerformance.UI/ViewModels/Navigation/NavigationGroup.cs`
  - `src/IICPerformance.UI/ViewModels/Navigation/NavigationViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Dashboard/DashboardViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Dashboard/ProjectSummaryCardViewModel.cs`
  - `src/IICPerformance.UI/Views/Controls/NavigationMenuControl.xaml`
  - `src/IICPerformance.UI/Views/Controls/NavigationMenuControl.xaml.cs`
  - `src/IICPerformance.UI/Views/Controls/DashboardView.xaml`
  - `src/IICPerformance.UI/Views/Controls/DashboardView.xaml.cs`
  - `src/IICPerformance.UI/Views/MainWindow.xaml` — update with full layout
  - `src/IICPerformance.UI/Views/MainWindow.xaml.cs`
- **Acceptance criteria:**
  - Left sidebar with 4 groups: Directories, Registers, Data Tables, Forms
  - Menu items filtered by current user access rights (no read -> hidden)
  - Clicking menu item opens corresponding window
  - Main area shows project summary dashboard (DevExpress tiles/cards)
  - Each project card shows: code, name, analysis versions count, last modified date
  - Layout matches screenshot style (sidebar + main content)
  - Navigation service handles window lifecycle

### TASK-010: Forms Scaffolding (6 form placeholders)
- **Priority:** Medium
- **Dependencies:** TASK-006, TASK-009
- **Complexity:** Moderate
- **Files to create:**
  - `src/IICPerformance.UI/ViewModels/Forms/DataUploadFormViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Forms/AssessmentFormViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Forms/PlanningFormViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Forms/VersionComparisonFormViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Forms/GroupProjectAnalysisFormViewModel.cs`
  - `src/IICPerformance.UI/ViewModels/Forms/GroupProjectTrendFormViewModel.cs`
  - `src/IICPerformance.UI/Views/Forms/DataUploadFormWindow.xaml`
  - `src/IICPerformance.UI/Views/Forms/AssessmentFormWindow.xaml`
  - `src/IICPerformance.UI/Views/Forms/PlanningFormWindow.xaml`
  - `src/IICPerformance.UI/Views/Forms/VersionComparisonFormWindow.xaml`
  - `src/IICPerformance.UI/Views/Forms/GroupProjectAnalysisFormWindow.xaml`
  - `src/IICPerformance.UI/Views/Forms/GroupProjectTrendFormWindow.xaml`
- **Acceptance criteria:**
  - All 6 forms inherit from FormViewModelBase
  - Each form opens as separate window or in main content area
  - DataUploadForm: project analysis version selector + file upload placeholder
  - Other forms: placeholder layout with title, description, empty content area
  - Forms accessible from navigation menu (filtered by permissions)
  - Placeholder designs — to be refined in later cycles

## Dependencies Graph

```
TASK-001 -> TASK-002 -> TASK-003 -> TASK-004 -> TASK-005 -> TASK-006 -> TASK-007
                                                                    -> TASK-008
TASK-005 -> TASK-009 -> TASK-010
TASK-006 -> TASK-009
```

## Execution Strategy

### Phase 1 (TASK-001 to TASK-004) — Foundation
Sequential execution. Each task depends on the previous one.

### Phase 2 (TASK-005 to TASK-006) — Auth & Access Control
Sequential. Auth must work before access control can be tested.

### Phase 3 (TASK-007 to TASK-008) — Data Windows
TASK-007 and TASK-008 can run in parallel (both depend on TASK-006).

### Phase 4 (TASK-009 to TASK-010) — UI Shell & Forms
Sequential. Main window navigation must exist before forms can be integrated.

## Architecture Decisions

- **Clean Architecture:** Domain -> Application -> Infrastructure -> UI (dependency flow inward)
- **DI:** Microsoft.Extensions.DependencyInjection in App.xaml.cs
- **MVVM:** CommunityToolkit.MVVM with source generators
- **UI Components:** DevExpress WPF (GridControl, TreeListControl, NavigationPane, LayoutControl)
- **Logging:** Serilog with rolling file + console sinks
- **Validation:** FluentValidation for all DTOs and edit dialogs
- **Mapping:** AutoMapper for Entity -> DTO conversion
- **Database:** EF Core with SQL Server, code-first migrations
- **Soft Delete:** IsArchived boolean flag on all entities
- **Session:** In-memory SessionService for current user (no token-based auth needed for desktop app)
- **Connection Settings:** Persisted to local encrypted storage (DPAPI)

## Implementation Notes

- All ViewModels use [ObservableProperty], [RelayCommand] from CommunityToolkit.MVVM
- DevExpress themes: use Office2019ColorfulWhite or similar enterprise theme
- Hierarchical data uses self-referencing ParentId pattern
- Archive toggle uses a single property on base ViewModelBase
- Navigation uses a simple service pattern (not Prism/MVVM Light)
- Exception handling: global handler in App.xaml.cs catches unhandled exceptions, logs via Serilog, shows user-friendly dialog
- DB connection failures: retry logic with exponential backoff for transient errors

## Progress (updated by orchestrator)
- [ ] TASK-001: Project Scaffolding & Solution Structure (Pending)
- [ ] TASK-002: Domain Models & Value Objects (Pending)
- [ ] TASK-003: Database Layer (EF Core) (Pending)
- [ ] TASK-004: Application Services & DTOs (Pending)
- [ ] TASK-005: Authentication System & Login Dialog (Pending)
- [ ] TASK-006: Access Control System & Base Infrastructure (Pending)
- [ ] TASK-007: Directory CRUD Windows (6 directories) (Pending)
- [ ] TASK-008: Register & Data Table Windows (Pending)
- [ ] TASK-009: Main Window with Navigation & Dashboard (Pending)
- [ ] TASK-010: Forms Scaffolding (6 form placeholders) (Pending)
