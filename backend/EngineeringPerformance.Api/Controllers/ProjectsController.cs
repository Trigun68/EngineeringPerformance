using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Security;
using EngineeringPerformance.Application.Projects.Commands;
using EngineeringPerformance.Application.Projects.DTOs;
using EngineeringPerformance.Application.Projects.Queries;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProjectsController : ControllerBase
{
    private readonly GetProjectsQueryHandler _getProjects;
    private readonly GetProjectByIdQueryHandler _getProjectById;
    private readonly CreateProjectCommandHandler _createProject;
    private readonly UpdateProjectCommandHandler _updateProject;
    private readonly ArchiveProjectCommandHandler _archiveProject;
    private readonly UnarchiveProjectCommandHandler _unarchiveProject;
    private readonly DeleteProjectCommandHandler _deleteProject;
    private readonly IProjectExportService _exportService;
    private readonly IProjectsCatalogExportService _catalogExportService;
    private readonly IVisualReportService _visualReports;
    private readonly IProjectImportService _importService;
    private readonly AppDbContext _context;

    public ProjectsController(
        GetProjectsQueryHandler getProjects,
        GetProjectByIdQueryHandler getProjectById,
        CreateProjectCommandHandler createProject,
        UpdateProjectCommandHandler updateProject,
        ArchiveProjectCommandHandler archiveProject,
        UnarchiveProjectCommandHandler unarchiveProject,
        DeleteProjectCommandHandler deleteProject,
        IProjectExportService exportService,
        IProjectsCatalogExportService catalogExportService,
        IVisualReportService visualReports,
        IProjectImportService importService,
        AppDbContext context)
    {
        _getProjects = getProjects;
        _getProjectById = getProjectById;
        _createProject = createProject;
        _updateProject = updateProject;
        _archiveProject = archiveProject;
        _unarchiveProject = unarchiveProject;
        _deleteProject = deleteProject;
        _exportService = exportService;
        _catalogExportService = catalogExportService;
        _visualReports = visualReports;
        _importService = importService;
        _context = context;
    }

    [HttpGet]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll(
        CancellationToken cancellationToken,
        [FromQuery] bool includeArchived = false)
    {
        var items = await _getProjects.HandleAsync(new GetProjectsQuery(includeArchived), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [RequireProjectAccess(ProjectIdRouteKey = "id")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<ActionResult<ProjectDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _getProjectById.HandleAsync(new GetProjectByIdQuery(id), cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var created = await _createProject.HandleAsync(new CreateProjectCommand(request), cancellationToken);
        return created is null ? Conflict("Project code already exists.") : CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectDto>> Update(int id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var updated = await _updateProject.HandleAsync(new UpdateProjectCommand(id, request), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("{id:int}/archive")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectDto>> Archive(int id, CancellationToken cancellationToken)
    {
        var archived = await _archiveProject.HandleAsync(new ArchiveProjectCommand(id), cancellationToken);
        return archived is null ? NotFound() : Ok(archived);
    }

    [HttpPost("{id:int}/unarchive")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectDto>> Unarchive(int id, CancellationToken cancellationToken)
    {
        var restored = await _unarchiveProject.HandleAsync(new UnarchiveProjectCommand(id), cancellationToken);
        return restored is null ? NotFound() : Ok(restored);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _deleteProject.HandleAsync(new DeleteProjectCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("export/excel")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportAllExcel(CancellationToken cancellationToken)
    {
        var bytes = await _catalogExportService.ExportAllToExcelAsync(cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "projects.xlsx");
    }

    [HttpGet("export/pdf")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportAllPdf(CancellationToken cancellationToken)
    {
        var bytes = await _catalogExportService.ExportAllToPdfAsync(cancellationToken);
        return File(bytes, "application/pdf", "projects.pdf");
    }

    [HttpGet("{id:int}/export/excel")]
    [RequireProjectAccess(ProjectIdRouteKey = "id")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportExcel(int id, CancellationToken cancellationToken)
    {
        var versionId = await _catalogExportService.GetFirstVersionIdAsync(id, cancellationToken);
        if (versionId is null)
            return NotFound("No analysis version found for project.");

        var bytes = await _exportService.ExportToExcelAsync(id, versionId.Value, cancellationToken: cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"project-{id}.xlsx");
    }

    [HttpGet("{id:int}/export/pdf")]
    [RequireProjectAccess(ProjectIdRouteKey = "id")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken cancellationToken)
    {
        var versionId = await _catalogExportService.GetFirstVersionIdAsync(id, cancellationToken);
        if (versionId is null)
            return NotFound("No analysis version found for project.");

        var bytes = await _exportService.ExportToPdfAsync(id, versionId.Value, cancellationToken: cancellationToken);
        return File(bytes, "application/pdf", $"project-{id}.pdf");
    }

    [HttpGet("{id:int}/export/combined.pdf")]
    [RequireProjectAccess(ProjectIdRouteKey = "id")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> ExportCombinedPdf(
        int id,
        [FromQuery] int planVersionId,
        [FromQuery] int factVersionId,
        [FromQuery] int compareVersionAId,
        [FromQuery] int compareVersionBId,
        [FromQuery] string formulaId = DocSetProportionalFormula.Id,
        [FromQuery] bool includeAppendix = false,
        CancellationToken cancellationToken = default)
    {
        // Validate that all versionIds belong to the requested project.
        var expectedProjectId = id;
        var versionIds = new[] { planVersionId, factVersionId, compareVersionAId, compareVersionBId };
        var versions = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => versionIds.Contains(v.Id))
            .Select(v => new { v.Id, v.ProjectId })
            .ToListAsync(cancellationToken);

        foreach (var vid in versionIds)
        {
            var found = versions.FirstOrDefault(v => v.Id == vid);
            if (found is null)
                throw new InvalidOperationException($"Версия с идентификатором {vid} не найдена.");
            if (found.ProjectId != expectedProjectId)
                throw new InvalidOperationException($"Версия {vid} принадлежит другому проекту.");
        }

        var bytes = await _visualReports.ExportCombinedPdfAsync(
            id,
            planVersionId,
            factVersionId,
            compareVersionAId,
            compareVersionBId,
            formulaId,
            includeAppendix,
            cancellationToken);
        return File(bytes, "application/pdf", $"Combined_{id}.pdf");
    }

    [HttpPost("import/excel")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    [RequestSizeLimit(ExcelUploadValidator.MaxFileSizeBytes)]
    public Task<IActionResult> ImportExcel(IFormFile file, CancellationToken cancellationToken)
    {
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Import projects from Excel is not implemented.",
            trackedAt = "https://github.com/orgs/engineering-performance/issues/legacy-import"
        }));
    }
}
