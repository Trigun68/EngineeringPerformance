using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Make PagedDirectoryResponse available without namespace prefix
using static EngineeringPerformance.Application.Directories.PagedDirectoryResponse;

namespace EngineeringPerformance.Api.Controllers;

/// <summary>
/// Контроллер для управления справочниками (directories).
/// Права доступа проверяются через [DirectoryPermission] — унифицированный подход с [HasPermission].
/// </summary>
[ApiController]
[Route("api/directories")]
[Authorize]
public sealed class DirectoriesController : ControllerBase
{
    private readonly IDirectoryFacade _directories;
    private readonly IDirectoryExportService _directoryExport;

    public DirectoriesController(IDirectoryFacade directories, IDirectoryExportService directoryExport)
    {
        _directories = directories;
        _directoryExport = directoryExport;
    }

    // === GENERIC DIRECTORY ROUTES ===
    // NOTE: regex constraint only on {resource}/{id:int} routes to avoid conflict with project-characteristics/{id}/options
    [HttpPost("{resource}/export")]
    [DirectoryPermission(AccessLevel.Read)]
    public IActionResult ExportExcelPost(string resource, [FromBody] DirectoryExportRequest request)
    {
        var bytes = _directoryExport.ExportToExcel(request);
        var safeName = SanitizeFileName(request.TabName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var filename = $"{safeName}_{timestamp}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    [HttpGet("{resource}/export.xlsx")]
    [DirectoryPermission(AccessLevel.Read)]
    public async Task<IActionResult> ExportExcel(string resource, CancellationToken cancellationToken)
    {
        var items = await _directories.ListAsync(resource, includeArchived: true, cancellationToken);
        var columns = new List<ReferenceExcelExporter.ColumnDef>
        {
            new("ID", x => ((DirectoryEntryDto)x).Id.ToString()),
            new("Name", x => ((DirectoryEntryDto)x).Name),
            new("ParentId", x => ((DirectoryEntryDto)x).ParentId?.ToString()),
            new("SortOrder", x => ((DirectoryEntryDto)x).SortOrder.ToString()),
            new("Archived", x => ((DirectoryEntryDto)x).IsArchived ? "Yes" : "No"),
        };

        var bytes = ReferenceExcelExporter.Export(resource, columns, items.Cast<object>());
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{resource}.xlsx");
    }

    [HttpGet("{resource}")]
    [DirectoryPermission(AccessLevel.Read)]
    public async Task<ActionResult<PagedDirectoryResponse>> List(
        string resource,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var allItems = await _directories.ListAsync(resource, includeArchived, cancellationToken);
        var totalCount = allItems.Count;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Ok(new PagedDirectoryResponse(items, totalCount, page, pageSize));
    }

    [HttpGet("{resource}/tree")]
    [DirectoryPermission(AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeNodeDto>>> Tree(
        string resource,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var tree = await _directories.GetTreeAsync(resource, includeArchived, cancellationToken);
            return Ok(tree);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{resource}/children")]
    [DirectoryPermission(AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<DirectoryEntryDto>>> Children(
        string resource,
        [FromQuery] int? parentId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var items = await _directories.GetChildrenAsync(resource, parentId, includeArchived, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{resource}/{id:int}")]
    [DirectoryPermission(AccessLevel.Read)]
    public async Task<ActionResult<DirectoryEntryDto>> GetById(string resource, int id, CancellationToken cancellationToken)
    {
        var item = await _directories.GetByIdAsync(resource, id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{resource}")]
    [DirectoryPermission(AccessLevel.Edit)]
    public async Task<ActionResult<DirectoryEntryDto>> Create(
        string resource,
        [FromBody] DirectoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _directories.CreateAsync(resource, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { resource, id = created.Id }, created);
    }

    [HttpPut("{resource}/{id:int}")]
    [DirectoryPermission(AccessLevel.Edit)]
    public async Task<ActionResult<DirectoryEntryDto>> Update(
        string resource,
        int id,
        [FromBody] DirectoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _directories.UpdateAsync(resource, id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{resource}/{id:int}/sort-order")]
    [DirectoryPermission(AccessLevel.Edit)]
    public async Task<IActionResult> UpdateSortOrder(
        string resource,
        int id,
        [FromBody] SortOrderRequest request,
        CancellationToken cancellationToken)
    {
        var ok = await _directories.UpdateSortOrderAsync(resource, id, request, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{resource}/{id:int}/archive")]
    [DirectoryPermission(AccessLevel.Edit)]
    public async Task<IActionResult> Archive(string resource, int id, CancellationToken cancellationToken)
    {
        var ok = await _directories.ArchiveAsync(resource, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{resource}/{id:int}/unarchive")]
    [DirectoryPermission(AccessLevel.Edit)]
    public async Task<IActionResult> Unarchive(string resource, int id, CancellationToken cancellationToken)
    {
        var ok = await _directories.UnarchiveAsync(resource, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{resource}/{id:int}")]
    [DirectoryPermission(AccessLevel.Edit)]
    public async Task<IActionResult> Delete(string resource, int id, CancellationToken cancellationToken)
    {
        var ok = await _directories.DeleteAsync(resource, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Справочник" : cleaned;
    }
}
