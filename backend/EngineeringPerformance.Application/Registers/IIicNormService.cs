namespace EngineeringPerformance.Application.Registers;

public interface IIicNormService
{
    Task<IReadOnlyList<IicNormDto>> GetAllAsync(DateTime? asOf, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IicNormDto>> GetHistoryAsync(int workDirectionId, CancellationToken cancellationToken = default);

    Task<IicNormDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IicNormDto> CreateAsync(IicNormWriteRequest request, CancellationToken cancellationToken = default);

    Task<IicNormDto?> UpdateAsync(int id, IicNormWriteRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
