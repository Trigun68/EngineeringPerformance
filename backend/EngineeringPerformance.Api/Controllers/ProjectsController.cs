using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Security;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Application.Projects.Commands;
using EngineeringPerformance.Application.Projects.DTOs;
using EngineeringPerformance.Application.Projects.Queries;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly IProjectExportService _exportService;
    private readonly IProjectsCatalogExportService _catalogExportService;
    private readonly IProjectImportService _importService;

    public ProjectsController(
        GetProjectsQueryHandler getProjects,
        GetProjectByIdQueryHandler getProjectById,
        CreateProjectCommandHandler createProject,
        UpdateProjectCommandHandler updateProject,
        ArchiveProjectCommandHandler archiveProject,
        IProjectExportService exportService,
        IProjectsCatalogExportService catalogExportService,
        IProjectImportService importService)
    {
        _getProjects = getProjects;
        _getProjectById = getProjectById;
        _createProject = createProject;
        _updateProject = updateProject;
        _archiveProject = archiveProject;
        _exportService = exportService;
        _catalogExportService = catalogExportService;
        _importService = importService;
    }

    [HttpGet]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _getProjects.HandleAsync(new GetProjectsQuery(), cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
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

    [HttpDelete("{id:int}")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    public async Task<ActionResult<ProjectDto>> Archive(int id, CancellationToken cancellationToken)
    {
        var archived = await _archiveProject.HandleAsync(new ArchiveProjectCommand(id), cancellationToken);
        return archived is null ? NotFound() : Ok(archived);
    }

    [HttpGet("export/excel")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportAllExcel(CancellationToken cancellationToken)
    {
        var bytes = await _catalogExportService.ExportAllToExcelAsync(cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "projects.xlsx");
    }

    [HttpGet("export/pdf")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportAllPdf(CancellationToken cancellationToken)
    {
        var bytes = await _catalogExportService.ExportAllToPdfAsync(cancellationToken);
        return File(bytes, "application/pdf", "projects.pdf");
    }

    [HttpGet("{id:int}/export/excel")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportExcel(int id, CancellationToken cancellationToken)
    {
        var versionId = await _catalogExportService.GetFirstVersionIdAsync(id, cancellationToken);
        if (versionId is null)
            return NotFound("No analysis version found for project.");

        var bytes = await _exportService.ExportToExcelAsync(id, versionId.Value, cancellationToken: cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"project-{id}.xlsx");
    }

    [HttpGet("{id:int}/export/pdf")]
    [HasPermission(PermissionType.Projects, AccessLevel.Read)]
    public async Task<IActionResult> ExportPdf(int id, CancellationToken cancellationToken)
    {
        var versionId = await _catalogExportService.GetFirstVersionIdAsync(id, cancellationToken);
        if (versionId is null)
            return NotFound("No analysis version found for project.");

        var bytes = await _exportService.ExportToPdfAsync(id, versionId.Value, cancellationToken: cancellationToken);
        return File(bytes, "application/pdf", $"project-{id}.pdf");
    }

    [HttpPost("import/excel")]
    [HasPermission(PermissionType.Projects, AccessLevel.Edit)]
    [RequestSizeLimit(ExcelUploadValidator.MaxFileSizeBytes)]
    public Task<IActionResult> ImportExcel(IFormFile file, CancellationToken cancellationToken)
    {
        // S0-02: Импорт списка проектов из Excel — не реализован (legacy WPF-функция).
        // Endpoint сохранён для совместимости, но возвращает 501.
        return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Import projects from Excel is not implemented.",
            trackedAt = "https://github.com/orgs/engineering-performance/issues/legacy-import"
        }));
    }
}
