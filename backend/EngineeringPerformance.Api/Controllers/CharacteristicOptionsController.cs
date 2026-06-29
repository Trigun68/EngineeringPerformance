using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Auth;
using EngineeringPerformance.Application.Directories;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

/// <summary>
/// Контроллер для управления вариантами значений характеристик проекта.
/// Вынесен в отдельный контроллер для избежания конфликтов маршрутизации.
/// </summary>
[ApiController]
[Route("api/characteristics")]
[Authorize]
public sealed class CharacteristicOptionsController : ControllerBase
{
    private readonly IDirectoryFacade _directories;

    public CharacteristicOptionsController(IDirectoryFacade directories)
    {
        _directories = directories;
    }

    [HttpGet("{id:int}/options")]
    [HasPermission(PermissionType.ProjectCharacteristics, AccessLevel.Read)]
    public async Task<ActionResult<IReadOnlyList<CharacteristicOptionDto>>> GetOptions(int id, CancellationToken cancellationToken)
    {
        var options = await _directories.GetCharacteristicOptionsAsync(id, cancellationToken);
        return Ok(options);
    }

    [HttpPost("{id:int}/options")]
    [HasPermission(PermissionType.ProjectCharacteristics, AccessLevel.Edit)]
    public async Task<ActionResult<CharacteristicOptionDto>> CreateOption(
        int id,
        [FromBody] CharacteristicOptionWriteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _directories.CreateCharacteristicOptionAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetOptions), new { id }, created);
    }

    [HttpPut("{id:int}/options/{optionId:int}")]
    [HasPermission(PermissionType.ProjectCharacteristics, AccessLevel.Edit)]
    public async Task<ActionResult<CharacteristicOptionDto>> UpdateOption(
        int id,
        int optionId,
        [FromBody] CharacteristicOptionWriteRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _directories.UpdateCharacteristicOptionAsync(id, optionId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}/options/{optionId:int}")]
    [HasPermission(PermissionType.ProjectCharacteristics, AccessLevel.Edit)]
    public async Task<IActionResult> DeleteOption(int id, int optionId, CancellationToken cancellationToken)
    {
        var ok = await _directories.DeleteCharacteristicOptionAsync(id, optionId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
