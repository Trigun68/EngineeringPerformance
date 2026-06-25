using EngineeringPerformance.Domain.Enums;

namespace EngineeringPerformance.Domain.Entities;

public sealed class DataImportIteration
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int ProjectAnalysisVersionId { get; set; }

    public DataImportType DataType { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public string? Description { get; set; }

    public int RecordCount { get; set; }

    /// <summary>FK на пользователя, инициировавшего импорт. NULL для исторических записей до V2-A.</summary>
    public int? ImportedByUserId { get; set; }

    /// <summary>Имя исходного файла (без пути) для аудита.</summary>
    public string? SourceFileName { get; set; }

    /// <summary>SHA-256 файла-источника, hex без разделителей. Используется для защиты от повторного импорта.</summary>
    public string? SourceFileHash { get; set; }

    public Project Project { get; set; } = null!;

    public ProjectAnalysisVersion ProjectAnalysisVersion { get; set; } = null!;

    public User? ImportedByUser { get; set; }
}
