using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.Services;

public interface IAccessControlService
{
    bool HasAccess(UserDto user, string objectName, AccessLevel requiredLevel);

    bool CanRead(UserDto user, string objectName);

    bool CanEdit(UserDto user, string objectName);

    IReadOnlyList<string> GetAccessibleObjects(UserDto user, AccessLevel level);

    Task<IReadOnlyList<AccessRightDto>> GetAllAccessRightsAsync();

    Task UpdateAccessRightAsync(AccessRightDto right);

    Task DeleteAccessRightAsync(int id);

    void ClearCache();
}
