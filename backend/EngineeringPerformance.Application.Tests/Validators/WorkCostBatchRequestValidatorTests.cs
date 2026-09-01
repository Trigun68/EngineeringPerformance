using EngineeringPerformance.Application.Registers;
using EngineeringPerformance.Application.Validators;
using EngineeringPerformance.Domain.Norms;
using FluentAssertions;
using FluentValidation;

namespace EngineeringPerformance.Application.Tests.Validators;

public sealed class WorkCostBatchRequestValidatorTests
{
    private readonly WorkCostBatchRequestValidator _validator = new();

    [Fact]
    public void Rejects_EmptyItems()
    {
        var request = new WorkCostBatchRequest(
            EffectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Level: WorkCostLevel.EachElement,
            Items: []);

        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void Rejects_NegativeRate()
    {
        var request = new WorkCostBatchRequest(
            EffectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Level: WorkCostLevel.EachElement,
            Items: [new WorkCostBatchItem(1, -1m)]);

        var result = _validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("RatePerHour", StringComparison.Ordinal));
    }

    [Fact]
    public void Accepts_ValidBatch()
    {
        var request = new WorkCostBatchRequest(
            EffectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Level: WorkCostLevel.EachElement,
            Items: [new WorkCostBatchItem(1, 1500m)]);

        _validator.Validate(request).IsValid.Should().BeTrue();
    }
}
