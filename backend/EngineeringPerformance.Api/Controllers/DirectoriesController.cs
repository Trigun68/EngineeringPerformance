using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Application.Permissions;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Infrastructure.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/directories")]
[Authorize]
public sealed class DirectoriesController : ControllerBase
{
    private readonly IDirectoryFacade _directories;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissions;

    public DirectoriesController(
        IDirectoryFacade directories,
        ICurrentUserService currentUser,
        IPermissionService permissions)
    {
        _directories = directories;
        _currentUser = currentUser;
        _permissions = permissions;
    }

    [HttpGet("{resource}/export.xlsx")]
    public async Task<IActionResult> ExportExcel(string resource, CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Read, cancellationToken);
        if (denied is not null)
            return denied;

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
    public async Task<ActionResult<IReadOnlyList<DirectoryEntryDto>>> List(
        string resource,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Read, cancellationToken);
        if (denied is not null)
            return denied;

        var items = await _directories.ListAsync(resource, includeArchived, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{resource}/tree")]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeNodeDto>>> Tree(
        string resource,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Read, cancellationToken);
        if (denied is not null)
            return denied;

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
    public async Task<ActionResult<IReadOnlyList<DirectoryEntryDto>>> Children(
        string resource,
        [FromQuery] int? parentId,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Read, cancellationToken);
        if (denied is not null)
            return denied;

        var items = await _directories.GetChildrenAsync(resource, parentId, includeArchived, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{resource}/{id:int}")]
    public async Task<ActionResult<DirectoryEntryDto>> GetById(string resource, int id, CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Read, cancellationToken);
        if (denied is not null)
            return denied;

        var item = await _directories.GetByIdAsync(resource, id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{resource}")]
    public async Task<ActionResult<DirectoryEntryDto>> Create(
        string resource,
        [FromBody] DirectoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var denied = await EnsureAccessAsync(resource, AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var created = await _directories.CreateAsync(resource, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { resource, id = created.Id }, created);
    }

    [HttpPut("{resource}/{id:int}")]
    public async Task<ActionResult<DirectoryEntryDto>> Update(
        string resource,
        int id,
        [FromBody] DirectoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var denied = await EnsureAccessAsync(resource, AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var updated = await _directories.UpdateAsync(resource, id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPatch("{resource}/{id:int}/sort-order")]
    public async Task<IActionResult> UpdateSortOrder(
        string resource,
        int id,
        [FromBody] SortOrderRequest request,
        CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var ok = await _directories.UpdateSortOrderAsync(resource, id, request, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{resource}/{id:int}/archive")]
    public async Task<IActionResult> Archive(string resource, int id, CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var ok = await _directories.ArchiveAsync(resource, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{resource}/{id:int}/unarchive")]
    public async Task<IActionResult> Unarchive(string resource, int id, CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var ok = await _directories.UnarchiveAsync(resource, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{resource}/{id:int}")]
    public async Task<IActionResult> Delete(string resource, int id, CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync(resource, AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var ok = await _directories.DeleteAsync(resource, id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpGet("project-characteristics/{id:int}/options")]
    public async Task<ActionResult<IReadOnlyList<CharacteristicOptionDto>>> GetOptions(int id, CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync("project-characteristics", AccessLevel.Read, cancellationToken);
        if (denied is not null)
            return denied;

        var options = await _directories.GetCharacteristicOptionsAsync(id, cancellationToken);
        return Ok(options);
    }

    [HttpPost("project-characteristics/{id:int}/options")]
    public async Task<ActionResult<CharacteristicOptionDto>> CreateOption(
        int id,
        [FromBody] CharacteristicOptionWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var denied = await EnsureAccessAsync("project-characteristics", AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var created = await _directories.CreateCharacteristicOptionAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetOptions), new { id }, created);
    }

    [HttpPut("project-characteristics/{id:int}/options/{optionId:int}")]
    public async Task<ActionResult<CharacteristicOptionDto>> UpdateOption(
        int id,
        int optionId,
        [FromBody] CharacteristicOptionWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var denied = await EnsureAccessAsync("project-characteristics", AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var updated = await _directories.UpdateCharacteristicOptionAsync(id, optionId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("project-characteristics/{id:int}/options/{optionId:int}")]
    public async Task<IActionResult> DeleteOption(int id, int optionId, CancellationToken cancellationToken)
    {
        var denied = await EnsureAccessAsync("project-characteristics", AccessLevel.Edit, cancellationToken);
        if (denied is not null)
            return denied;

        var ok = await _directories.DeleteCharacteristicOptionAsync(id, optionId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    private async Task<ActionResult?> EnsureAccessAsync(string resource, AccessLevel level, CancellationToken cancellationToken)
    {
        if (!DirectoryResourceCatalog.TryGet(resource, out var meta))
            return NotFound();

        if (_currentUser.UserId is not int userId)
            return Unauthorized();

        if (!await _permissions.HasAccessAsync(userId, meta.Permission, level, cancellationToken))
            return Forbid();

        return null;
    }
}
