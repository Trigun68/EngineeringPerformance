using EngineeringPerformance.Application.Norms;
using EngineeringPerformance.Domain.Norms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public sealed class NormsController : ControllerBase
{
    private readonly INormCalculator _normCalculator;
    private readonly IDeviationCalculator _deviationCalculator;

    public NormsController(INormCalculator normCalculator, IDeviationCalculator deviationCalculator)
    {
        _normCalculator = normCalculator;
        _deviationCalculator = deviationCalculator;
    }

    [HttpGet("formula-templates")]
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
    public ActionResult<object> Preview([FromBody] NormPreviewRequest request)
    {
        var normFormula = NormDtoMapper.ToEntity(request.NormFormula);
        var deviationFormula = NormDtoMapper.ToEntity(request.DeviationFormula);

        if (normFormula.Type == 0)
            normFormula.Type = FormulaType.Linear;
        if (normFormula.MaxVolume <= 0)
            normFormula.MaxVolume = 999_999m;
        if (deviationFormula.ModelType == 0)
            deviationFormula.ModelType = DeviationModelType.Multiplicative;

        var band = _deviationCalculator.GenerateBand(normFormula, deviationFormula, request.Points ?? 100);
        var curve = _normCalculator.GenerateCurve(normFormula, request.Points ?? 100)
            .Select(p => new { volume = p.V, n = p.N });

        return Ok(new
        {
            curve,
            band = band.Select(b => new
            {
                volume = b.Volume,
                center = b.Center,
                upper = b.Upper,
                lower = b.Lower,
            }),
        });
    }
}
