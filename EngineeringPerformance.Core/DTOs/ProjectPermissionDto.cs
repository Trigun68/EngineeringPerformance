using EngineeringPerformance.Core.Enums;

namespace EngineeringPerformance.Core.DTOs;

/// <summary>
/// V2-C: пользовательский слой просмотра разрешений на проект. UI рисует матрицу
/// «пользователь × уровень» по списку этих DTO.
/// </summary>
public sealed record ProjectPermissionDto(
    int UserId,
    string Username,
    string DisplayName,
    string RoleName,
    AccessLevel Level);
