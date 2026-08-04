namespace EngineeringPerformance.Domain.Entities;

/// <summary>Метрика одного SQL-запроса в контексте HTTP-запроса (внутренний profiling).</summary>
public sealed class SqlQueryMetric
{
    public long Id { get; set; }

    public DateTime TimestampUtc { get; set; }

    /// <summary>Маршрут / DisplayName эндпоинта.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>HTTP method + path template (если доступен).</summary>
    public string? RequestPath { get; set; }

    /// <summary>SELECT / INSERT / UPDATE / DELETE / OTHER.</summary>
    public string QueryType { get; set; } = "OTHER";

    /// <summary>Таблицы, затронутые командой (через запятую).</summary>
    public string? TablesInvolved { get; set; }

    public double DurationMs { get; set; }

    /// <summary>Строки прочитанные (SELECT) или затронутые (DML).</summary>
    public int? RowsReturned { get; set; }

    public int? RowsAffected { get; set; }

    /// <summary>SHA-256 нормализованного текста SQL (группировка одинаковых запросов).</summary>
    public string QueryHash { get; set; } = string.Empty;

    /// <summary>Укороченный текст SQL (без литералов параметров).</summary>
    public string? SqlPreview { get; set; }

    /// <summary>JSON параметров SQL-команды.</summary>
    public string? ParametersJson { get; set; }

    /// <summary>JSON фильтров/сортировки/пагинации HTTP-запроса.</summary>
    public string? RequestParametersJson { get; set; }

    /// <summary>Общее время HTTP-запроса (заполняется при flush, мс).</summary>
    public double? RequestDurationMs { get; set; }

    public string? CorrelationId { get; set; }
}
