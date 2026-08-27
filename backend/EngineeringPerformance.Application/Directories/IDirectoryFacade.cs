namespace EngineeringPerformance.Application.Directories;

public interface IDirectoryFacade
{
    Task<IReadOnlyList<DirectoryEntryDto>> ListAsync(
        string resource,
        bool includeArchived,
        CancellationToken cancellationToken,
        int? projectId = null);

    Task<IReadOnlyList<DirectoryEntryDto>> GetChildrenAsync(
        string resource,
        int? parentId,
        bool includeArchived,
        CancellationToken cancellationToken,
        int? projectId = null);

    Task<IReadOnlyList<DirectoryTreeNodeDto>> GetTreeAsync(
        string resource,
        bool includeArchived,
        CancellationToken cancellationToken,
        int? projectId = null);

    Task<DirectoryEntryDto?> GetByIdAsync(string resource, int id, CancellationToken cancellationToken);

    Task<DirectoryEntryDto> CreateAsync(string resource, DirectoryWriteRequest request, CancellationToken cancellationToken);

    Task<DirectoryEntryDto?> UpdateAsync(string resource, int id, DirectoryWriteRequest request, CancellationToken cancellationToken);

    Task<bool> ArchiveAsync(string resource, int id, CancellationToken cancellationToken);

    Task<bool> UnarchiveAsync(string resource, int id, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string resource, int id, CancellationToken cancellationToken);

    Task<bool> UpdateSortOrderAsync(string resource, int id, SortOrderRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<CharacteristicOptionDto>> GetCharacteristicOptionsAsync(int characteristicId, CancellationToken cancellationToken);

    Task<CharacteristicOptionDto> CreateCharacteristicOptionAsync(int characteristicId, CharacteristicOptionWriteRequest request, CancellationToken cancellationToken);

    Task<CharacteristicOptionDto?> UpdateCharacteristicOptionAsync(int characteristicId, int optionId, CharacteristicOptionWriteRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteCharacteristicOptionAsync(int characteristicId, int optionId, CancellationToken cancellationToken);
}
