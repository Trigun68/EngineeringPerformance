using FluentValidation;
using EngineeringPerformance.Application.Import;

namespace EngineeringPerformance.Application.Validators;

public sealed class PhysicalVolumeIngestRequestValidator : AbstractValidator<PhysicalVolumeIngestRequest>
{
    public PhysicalVolumeIngestRequestValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleFor(x => x.Mode).Must(IngestModeRules.IsKnown).WithMessage(IngestModeRules.Message);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class EquipmentIngestRequestValidator : AbstractValidator<EquipmentIngestRequest>
{
    public EquipmentIngestRequestValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleFor(x => x.Mode).Must(IngestModeRules.IsKnown).WithMessage(IngestModeRules.Message);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity).GreaterThanOrEqualTo(0);
        });
    }
}

public sealed class LaborIntensityIngestRequestValidator : AbstractValidator<LaborIntensityIngestRequest>
{
    public LaborIntensityIngestRequestValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleFor(x => x.Mode).Must(IngestModeRules.IsKnown).WithMessage(IngestModeRules.Message);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DocumentTypeName).NotEmpty().WithMessage("Тип документа обязателен.");
            item.RuleFor(i => i.PlannedHours).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.ActualHours).GreaterThanOrEqualTo(0);
        });
    }
}

internal static class IngestModeRules
{
    public const string Message = "Режим ingest: replace или upsert.";

    public static bool IsKnown(string? mode) =>
        string.IsNullOrWhiteSpace(mode)
        || mode.Equals("replace", StringComparison.OrdinalIgnoreCase)
        || mode.Equals("upsert", StringComparison.OrdinalIgnoreCase);
}
