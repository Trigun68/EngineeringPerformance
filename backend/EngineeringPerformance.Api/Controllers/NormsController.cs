using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Enums;
using EngineeringPerformance.Domain.Norms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class NormsController : ControllerBase
{
    private readonly INormPreviewService _normPreviewService;

    public NormsController(INormPreviewService normPreviewService)
    {
        _normPreviewService = normPreviewService;
    }

    [HttpGet("formula-templates")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public ActionResult<object> GetFormulaTemplates()
    {
        var templates = FormulaTemplates.All.Select(t => new
        {
            type = (int)t.Type,
            displayName = t.DisplayName,
            formulaLatex = t.FormulaLatex,
            requiresBounds = t.RequiresBounds,
            coefficients = t.Coefficients.Select(c => new
            {
                name = c.Name,
                label = c.Label,
                required = c.Required,
                min = c.Min,
                max = c.Max,
            }),
        }).ToList();

        return Ok(templates);
    }

    [HttpPost("norms/preview")]
    [HasPermission(PermissionType.IicWorkDirectionNorms, AccessLevel.Read)]
    public ActionResult<NormPreviewResult> Preview([FromBody] NormPreviewRequest request) =>
        Ok(_normPreviewService.Preview(request));
}
