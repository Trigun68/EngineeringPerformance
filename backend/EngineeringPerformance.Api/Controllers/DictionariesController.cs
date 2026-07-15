using System.Diagnostics;
using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Diagnostics;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DictionariesController : ControllerBase
{
    private readonly IDictionaryService _dictionaryService;

    public DictionariesController(IDictionaryService dictionaryService)
    {
        _dictionaryService = dictionaryService;
    }

    [HttpGet("all")]
    [HasPermission(PermissionType.References, AccessLevel.Read)]
    public async Task<ActionResult<DictionariesAllDto>> GetAll(CancellationToken cancellationToken)
    {
        // #region agent log
        var sw = Stopwatch.StartNew();
        DebugSessionLog.Write("B", "DictionariesController.GetAll", "start", null);
        // #endregion
        var result = await _dictionaryService.GetAllAsync(cancellationToken);
        // #region agent log
        sw.Stop();
        DebugSessionLog.Write(
            "B",
            "DictionariesController.GetAll",
            "done",
            new
            {
                elapsedMs = sw.ElapsedMilliseconds,
                pvTypes = result.PhysicalVolumeTypes.Count,
                directions = result.IicWorkDirections.Count,
            });
        // #endregion
        return Ok(result);
    }
}
