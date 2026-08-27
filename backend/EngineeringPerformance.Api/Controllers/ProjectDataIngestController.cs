using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

/// <summary>
/// JSON-пакет данных версии (дубль Excel-импорта). Маршруты <c>…/import/*</c> не меняются.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:int}/versions/{versionId:int}/ingest")]
[Authorize]
[HasPermission(PermissionType.ProjectDataUpload, AccessLevel.Edit)]
public sealed class ProjectDataIngestController : ControllerBase
{
    private readonly IProjectImportService _import;

    public ProjectDataIngestController(IProjectImportService import)
    {
        _import = import;
    }

    /// <summary>Пакет физ. объёмов по шифру ЕП или Id номенклатуры.</summary>
    [HttpPost("physical-volumes")]
    [RequireProjectAccess(AccessLevel.Edit)]
    public Task<IActionResult> IngestPhysicalVolumes(
        int projectId,
        int versionId,
        [FromBody] PhysicalVolumeIngestRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Task.FromResult<IActionResult>(BadRequest("Body is required."));
        return ExecuteAsync(
            () => _import.IngestPhysicalVolumesAsync(projectId, versionId, request, cancellationToken),
            "physical-volumes",
            versionId);
    }

    /// <summary>Пакет оборудования по имени каталога или equipmentId.</summary>
    [HttpPost("equipment")]
    [RequireProjectAccess(AccessLevel.Edit)]
    public Task<IActionResult> IngestEquipment(
        int projectId,
        int versionId,
        [FromBody] EquipmentIngestRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Task.FromResult<IActionResult>(BadRequest("Body is required."));
        return ExecuteAsync(
            () => _import.IngestEquipmentAsync(projectId, versionId, request, cancellationToken),
            "equipment",
            versionId);
    }

    /// <summary>Пакет трудоёмкости по DocumentTypeName.</summary>
    [HttpPost("labor-intensities")]
    [RequireProjectAccess(AccessLevel.Edit)]
    public Task<IActionResult> IngestLabor(
        int projectId,
        int versionId,
        [FromBody] LaborIntensityIngestRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Task.FromResult<IActionResult>(BadRequest("Body is required."));
        return ExecuteAsync(
            () => _import.IngestLaborIntensitiesAsync(projectId, versionId, request, cancellationToken),
            "labor-intensities",
            versionId);
    }

    /// <summary>Предпросмотр: типы документов, которых нет в справочнике.</summary>
    [HttpPost("labor-intensities/preview")]
    [RequireProjectAccess(AccessLevel.Read)]
    public async Task<IActionResult> PreviewLabor(
        int projectId,
        int versionId,
        [FromBody] LaborIntensityIngestPreviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Body is required.");
        _ = projectId;
        _ = versionId;
        var preview = await _import.PreviewLaborIngestAsync(request.DocumentTypeNames ?? [], cancellationToken);
        return Ok(new
        {
            unmatchedDocumentTypes = preview.UnmatchedDocumentTypes,
            totalDataRows = preview.TotalDataRows,
        });
    }

    private static async Task<IActionResult> ExecuteAsync(
        Func<Task<DataImportResultDto>> action,
        string dataType,
        int versionId)
    {
        var result = await action();
        if (!result.Success)
            return new BadRequestObjectResult(new { result.ErrorMessage, result.IterationId, rowErrors = result.RowErrors });

        return new OkObjectResult(new
        {
            dataType,
            versionId,
            importedRows = result.RecordsImported,
            iterationId = result.IterationId,
            details = result.SuccessDetails,
            rowErrors = result.RowErrors,
        });
    }
}
