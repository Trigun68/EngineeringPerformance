namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>Общие строки шапки табличных Excel/PDF: проект, версия анализа, дата.</summary>
internal static class TabularReportBanner
{
    public static IReadOnlyList<string> Lines(string projectCode, string projectName, string versionName)
    {
        var project = string.IsNullOrWhiteSpace(projectCode)
            ? projectName
            : string.IsNullOrWhiteSpace(projectName)
                ? projectCode
                : $"{projectCode} — {projectName}";

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(project))
            lines.Add($"Проект: {project}");
        if (!string.IsNullOrWhiteSpace(versionName))
            lines.Add($"Версия анализа: {versionName}");
        lines.Add($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}");
        return lines;
    }
}
