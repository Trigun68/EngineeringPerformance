namespace EngineeringPerformance.Infrastructure.Data;

/// <summary>
/// Привязка проекта к выбранному варианту характеристики.
/// Уникально по (ProjectId, CharacteristicId): одна характеристика может быть
/// выбрана для проекта только один раз.
/// </summary>
public sealed class ProjectCharacteristicSelection
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int CharacteristicId { get; set; }

    public int OptionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    public Project? Project { get; set; }
    public ProjectCharacteristic? Characteristic { get; set; }
    public ProjectCharacteristicOption? Option { get; set; }
}
