namespace EngineeringPerformance.Application.LaborIntensity;

public interface ILaborIntensityIicBindingService
{
    Task<IReadOnlyList<LaborIntensityIicBindingDto>> ListAsync(
        int laborIntensityId,
        CancellationToken cancellationToken = default);

    Task<LaborIntensityIicBindingDto?> CreateAsync(
        int laborIntensityId,
        LaborIntensityIicBindingWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<LaborIntensityIicBindingDto?> UpdateAsync(
        int id,
        LaborIntensityIicBindingWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
