using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Api.Authorization;
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
        var result = await _dictionaryService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
