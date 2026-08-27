namespace EngineeringPerformance.Application.Dictionaries;

public interface IDictionaryService
{
    Task<DictionariesAllDto> GetAllAsync(CancellationToken cancellationToken = default);

    Task<(DictionariesAllDto Data, string ETag)> GetAllWithETagAsync(CancellationToken cancellationToken = default);

    Task<bool> UnitExistsAsync(int unitId, CancellationToken cancellationToken = default);

    Task<bool> DirectionExistsAsync(int directionId, CancellationToken cancellationToken = default);

    Task<bool> NormTypeExistsAsync(string normType, CancellationToken cancellationToken = default);
}
