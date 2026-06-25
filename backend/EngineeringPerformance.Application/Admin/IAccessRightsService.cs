using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Application.Admin;

public interface IAccessRightsService
{
    Task<IReadOnlyList<AccessRightDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(int id, AccessLevel accessLevel, CancellationToken cancellationToken = default);

    Task<PermissionMatrixDto> GetMatrixAsync(CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UpdateMatrixAsync(
        PermissionMatrixUpdateRequest request,
        CancellationToken cancellationToken = default);
}
