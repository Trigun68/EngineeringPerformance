using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Security;
using EngineeringPerformance.Application.Import;
using EngineeringPerformance.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EngineeringPerformance.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/versions/{versionId:int}/import")]
[Authorize]
[HasPermission(PermissionType.ProjectDataUpload, AccessLevel.Edit)]
public sealed class ImportController : ControllerBase
{
    private readonly IProjectImportService _import;

    public ImportController(IProjectImportService import)
    {
        _import = import;
    }

    [HttpPost("{dataType}")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [RequestSizeLimit(ExcelUploadValidator.MaxFileSizeBytes)]
    public async Task<IActionResult> Import(
        int projectId,
        int versionId,
        string dataType,
        [FromForm] IFormFile file,
        [FromQuery] string? description,
        [FromQuery] bool addMissingLaborToCatalog = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDataType(dataType, out var importType))
            return BadRequest("Invalid dataType. Use physical-volumes, labor-intensities, or equipment.");

        if (!ExcelUploadValidator.TryValidate(file, out var error))
            return BadRequest(error);

        await using var stream = file.OpenReadStream();
        var result = await _import.ImportProjectDataAsync(
            projectId,
            versionId,
            importType,
            stream,
            ExcelUploadValidator.SanitizeFileName(file.FileName),
            description,
            addMissingLaborToCatalog,
            file.ContentType,
            file.Length,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { result.ErrorMessage, result.IterationId, rowErrors = result.RowErrors });

        return Ok(new
        {
            dataType,
            versionId,
            importedRows = result.RecordsImported,
            iterationId = result.IterationId,
            details = result.SuccessDetails,
            rowErrors = result.RowErrors,
        });
    }

    /// <summary>POST /api/projects/{projectId}/versions/{versionId}/import/equipment</summary>
    [HttpPost("equipment")]
    [RequireProjectAccess(AccessLevel.Edit)]
    [RequestSizeLimit(ExcelUploadValidator.MaxFileSizeBytes)]
    public Task<IActionResult> ImportEquipment(
        int projectId,
        int versionId,
        [FromForm] IFormFile file,
        [FromQuery] string? description,
        CancellationToken cancellationToken = default) =>
        Import(projectId, versionId, "equipment", file, description, addMissingLaborToCatalog: false, cancellationToken);

    [HttpPost("labor-intensities/preview")]
    [RequireProjectAccess(AccessLevel.Read)]
    [RequestSizeLimit(ExcelUploadValidator.MaxFileSizeBytes)]
    public async Task<IActionResult> PreviewLabor(
        int projectId,
        int versionId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (!ExcelUploadValidator.TryValidate(file, out var error))
            return BadRequest(error);

        await using var stream = file.OpenReadStream();
        var preview = await _import.PreviewLaborImportAsync(stream, cancellationToken);
        return Ok(new
        {
            unmatchedDocumentTypes = preview.UnmatchedDocumentTypes,
            totalDataRows = preview.TotalDataRows,
        });
    }

    [HttpGet("{dataType}/iterations")]
    [RequireProjectAccess(AccessLevel.Read)]
    public async Task<IActionResult> ListIterations(
        int projectId,
        int versionId,
        string dataType,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDataType(dataType, out var importType))
            return BadRequest("Invalid dataType.");

        var items = await _import.GetImportIterationsAsync(projectId, versionId, importType, cancellationToken);
        return Ok(items);
    }

    [HttpDelete("{dataType}/iterations")]
    [RequireProjectAccess(AccessLevel.Edit)]
    public async Task<IActionResult> RollbackIteration(
        int projectId,
        int versionId,
        string dataType,
        [FromQuery] DateTime importedAt,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseDataType(dataType, out var importType))
            return BadRequest("Invalid dataType.");

        var deleted = await _import.DeleteIterationAsync(projectId, versionId, importType, importedAt, cancellationToken);
        return deleted > 0 ? Ok(new { deletedRecords = deleted }) : NotFound();
    }

    private static bool TryParseDataType(string dataType, out DataImportType importType) =>
        dataType switch
        {
            "physical-volumes" => Assign(DataImportType.PhysicalVolume, out importType),
            "labor-intensities" => Assign(DataImportType.LaborIntensity, out importType),
            "equipment" => Assign(DataImportType.Equipment, out importType),
            _ => Assign(default, out importType) && false,
        };

    private static bool Assign(DataImportType value, out DataImportType importType)
    {
        importType = value;
        return true;
    }
}
