using System.Text;

namespace EngineeringPerformance.Application.Export;

/// <summary>
/// Единый формат имён файлов экспорта (Excel / PDF / CSV).
/// Шаблон: {модуль}_{вкладка}[_{уточнение}]_-_{код}_{название}_({версия}).{ext}
/// </summary>
public static class ExportFileNameBuilder
{
    public const int MaxFileNameLength = 180;

    public static class Modules
    {
        public const string Forecast = "прогноз";
        public const string Fact = "факт";
        public const string Compare = "сравнение";
        public const string Report = "отчет";
        public const string Directory = "справочник";
        public const string Analytics = "аналитика";
        public const string Audit = "аудит";
        public const string Projects = "проекты";
    }

    public static class Views
    {
        public const string Summary = "итого";
        public const string Facility = "по_объектам";
        public const string Detail = "детально";
        public const string Analysis = "анализ";
        public const string Version = "версия";
        public const string Combined = "сводный";
        public const string Catalog = "каталог";
        public const string AuditLog = "журнал_действий";
        public const string Norms = "нормы";
        public const string NormHealth = "здоровье_норм";
        public const string FactNorm = "факт_норма";
        public const string Trends = "динамика";
        public const string Trend = "тренд";
        public const string ProductivityTrend = "тренд_производительности";
        public const string IicGroups = "группы_IIC";
        public const string IicNorms = "нормы_IIC";
    }

    public static class Qualifiers
    {
        public const string Visible = "выделенные";
        public const string SummaryTable = "сводная";
    }

    /// <summary>
    /// Собирает имя файла. Без проекта блок <c>_-_код_название</c> опускается; без версии — скобки.
    /// </summary>
    public static string Build(
        string module,
        string extension,
        string? view = null,
        string? qualifier = null,
        string? projectCode = null,
        string? projectName = null,
        string? versionLabel = null)
    {
        var report = JoinTokens(
            SanitizeToken(module) ?? Modules.Report,
            SanitizeToken(view),
            SanitizeToken(qualifier)) ?? Modules.Report;

        var code = SanitizeToken(projectCode, keepHyphen: true);
        var name = SanitizeToken(projectName);
        var version = SanitizeToken(versionLabel);
        var ext = NormalizeExtension(extension);

        var stem = BuildStem(report, code, name, version);
        return Truncate(stem, code, name, version, report, ext) + "." + ext;
    }

    public static string? CombineVersions(params string?[] names)
    {
        if (names.Length == 0)
            return null;

        var parts = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var token = SanitizeToken(name);
            if (token is null || !seen.Add(token))
                continue;
            parts.Add(token);
        }

        return parts.Count == 0 ? null : string.Join('_', parts);
    }

    /// <summary>Вкладка сравнения: summary / facility / detail / full.</summary>
    public static string? CompareView(string? section) =>
        section?.Trim().ToLowerInvariant() switch
        {
            "summary" => Views.Summary,
            "facility" => Views.Facility,
            "detail" => Views.Detail,
            "full" => null,
            _ => SanitizeToken(section),
        };

    /// <summary>Русское имя вкладки справочника по slug или уже готовому заголовку.</summary>
    public static string DirectoryView(string? tabOrSlug) =>
        tabOrSlug?.Trim().ToLowerInvariant() switch
        {
            "physical-volumes-kfo" => "ФО_KFO",
            "physical-volumes-ber" => "ФО_BER",
            "equipment" => "оборудование",
            "labor-intensities" => "трудоемкость",
            "iic-work-directions" => Views.IicGroups,
            "units-of-measurement" => "единицы",
            "project-characteristics" => "характеристики",
            "iic-work-direction-norms" => Views.IicNorms,
            _ => SanitizeToken(tabOrSlug) ?? Views.Catalog,
        };

    public static string? SanitizeToken(string? value, bool keepHyphen = true)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var builder = new StringBuilder(value.Length);
        var previousUnderscore = false;
        foreach (var ch in value.Trim())
        {
            if (IsAllowed(ch, keepHyphen))
            {
                builder.Append(ch);
                previousUnderscore = false;
                continue;
            }

            if (previousUnderscore)
                continue;

            builder.Append('_');
            previousUnderscore = true;
        }

        var cleaned = builder.ToString().Trim('_');
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }

    private static string BuildStem(string report, string? code, string? name, string? version)
    {
        var stem = report;
        var project = JoinTokens(code, name);
        if (project is not null)
            stem += "_-_" + project;
        if (version is not null)
            stem += "_(" + version + ")";
        return stem;
    }

    private static string Truncate(
        string stem,
        string? code,
        string? name,
        string? version,
        string report,
        string extension)
    {
        var maxStem = MaxFileNameLength - extension.Length - 1;
        if (stem.Length <= maxStem)
            return stem;

        if (name is not null)
        {
            var reserved = report.Length
                + (code is null ? 0 : 3 + code.Length)
                + 1
                + (version is null ? 0 : 3 + version.Length);
            var nameBudget = maxStem - reserved;
            if (nameBudget >= 1)
            {
                var trimmedName = name[..Math.Min(name.Length, nameBudget)].TrimEnd('_');
                if (trimmedName.Length > 0)
                {
                    var retry = BuildStem(report, code, trimmedName, version);
                    if (retry.Length <= maxStem)
                        return retry;
                }
            }

            stem = BuildStem(report, code, null, version);
            if (stem.Length <= maxStem)
                return stem;
        }

        return stem[..maxStem].TrimEnd('_', '-', '(');
    }

    private static string? JoinTokens(params string?[] parts)
    {
        var present = parts.Where(p => !string.IsNullOrEmpty(p)).ToArray();
        return present.Length == 0 ? null : string.Join('_', present);
    }

    private static string NormalizeExtension(string? extension)
    {
        var ext = (extension ?? "xlsx").Trim().TrimStart('.');
        ext = SanitizeToken(ext) ?? "xlsx";
        return ext.ToLowerInvariant();
    }

    private static bool IsAllowed(char ch, bool keepHyphen)
    {
        if (ch is '(' or ')')
            return false;
        if (char.IsWhiteSpace(ch))
            return false;
        if (ch is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
            return false;
        if (ch == '.')
            return false;
        if (ch == '-' && keepHyphen)
            return true;
        if (ch is '_' or '—')
            return ch == '_';
        return !Path.GetInvalidFileNameChars().Contains(ch);
    }
}
