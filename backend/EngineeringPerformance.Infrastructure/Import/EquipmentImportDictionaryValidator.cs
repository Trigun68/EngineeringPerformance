using EngineeringPerformance.Application.Dictionaries;
using EngineeringPerformance.Application.Import;

namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>Проверка ссылок на справочники (UnitId, DirectionId, NormType) перед upsert оборудования.</summary>
internal sealed class EquipmentImportDictionaryValidator
{
    private readonly IDictionaryService _dictionaryService;

    public EquipmentImportDictionaryValidator(IDictionaryService dictionaryService)
    {
        _dictionaryService = dictionaryService;
    }

    public async Task<ImportRowErrorDto?> ValidateRowAsync(
        int rowNumber,
        EquipmentDictionaryRefs refs,
        EquipmentDictionaryColumnFlags columns,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (columns.ValidateUnitId && refs.UnitId is int unitId
            && !await _dictionaryService.UnitExistsAsync(unitId, cancellationToken))
        {
            errors.Add("Unit not found");
        }

        if (columns.ValidateDirectionId && refs.DirectionId is int directionId
            && !await _dictionaryService.DirectionExistsAsync(directionId, cancellationToken))
        {
            errors.Add("Direction not found");
        }

        if (columns.ValidateNormType && !string.IsNullOrWhiteSpace(refs.NormType)
            && !await _dictionaryService.NormTypeExistsAsync(refs.NormType, cancellationToken))
        {
            errors.Add("NormType not found");
        }

        return errors.Count > 0
            ? new ImportRowErrorDto(rowNumber, string.Join("; ", errors))
            : null;
    }

    internal readonly record struct EquipmentDictionaryRefs(int? UnitId, int? DirectionId, string? NormType);

    internal readonly record struct EquipmentDictionaryColumnFlags(
        bool ValidateUnitId,
        bool ValidateDirectionId,
        bool ValidateNormType);
}
