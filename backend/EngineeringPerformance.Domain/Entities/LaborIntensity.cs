using EngineeringPerformance.Domain.Common;

namespace EngineeringPerformance.Domain.Entities;

public sealed class LaborIntensity : DirectoryItem
{
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// «Тип документа» — ключ связи со строками <see cref="ProjectLaborIntensity"/>
    /// (Excel-колонка «Наименование типа документа TF»). Нормализуется Trim/OrdinalIgnoreCase.
    /// </summary>
    public string? DocumentTypeName { get; set; }

    public decimal? NormPpr { get; set; }

    public decimal? NormOpr { get; set; }

    public decimal? NormPd { get; set; }

    public decimal? NormRd { get; set; }

    public int? IicWorkDirectionId { get; set; }

    /// <summary>
    /// «Техас» — исходный код из файла-источника справочника, используется для детерминированного
    /// сопоставления с IicWorkDirection (см. `IIC-{Техас через дефис без завершающей точки}`).
    /// </summary>
    public string? Techas { get; set; }

    public LaborIntensity? Parent { get; set; }

    public ICollection<LaborIntensity> Children { get; set; } = [];

    public IicWorkDirection? IicWorkDirection { get; set; }
}
