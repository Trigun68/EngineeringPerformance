using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

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
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var (result, etag) = await _dictionaryService.GetAllWithETagAsync(cancellationToken);
        var parsed = EntityTagHeaderValue.Parse(etag);
        if (Request.GetTypedHeaders().IfNoneMatch?.Any(t => !t.IsWeak && t.Tag == parsed.Tag) == true)
        {
            Response.Headers.ETag = etag;
            Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
            {
                Private = true,
                MustRevalidate = true,
            };
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;
        Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Private = true,
            MustRevalidate = true,
        };
        return Ok(result);
    }
}
