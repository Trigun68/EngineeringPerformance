using EngineeringPerformance.Core.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace EngineeringPerformance.Infrastructure.Data;

public sealed class Project : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    public string? WorkNumber { get; set; }

    public string? ContractNumber { get; set; }

    public string? CalendarPlanNumber { get; set; }

    public string? Type { get; set; }

    public string? Customer { get; set; }

    public string? Contractor { get; set; }

    public string? Gip { get; set; }

    public string? WorkStatus { get; set; }

    /// <summary>Дата начала работ по проекту. V2-A.1: конвертировано из nvarchar в date.</summary>
    public DateOnly? WorkStart { get; set; }

    /// <summary>Дата окончания работ по проекту. Добавлено в V2-A.</summary>
    public DateOnly? WorkEnd { get; set; }

    /// <summary>Дата контракта. Добавлено в V2-A.</summary>
    public DateOnly? ContractDate { get; set; }

    public string? Program { get; set; }

    [NotMapped]
    public int VersionCount { get; set; }

    public ICollection<ProjectAnalysisVersion> AnalysisVersions { get; set; } = [];

    /// <summary>V2-C: индивидуальные разрешения на проект. Заменяет ProjectUserAccess + Visibility.</summary>
    public ICollection<ProjectPermission> Permissions { get; set; } = [];
}
