using ClosedXML.Excel;
using EngineeringPerformance.Core.DTOs;
using EngineeringPerformance.Core.Services;

namespace EngineeringPerformance.Infrastructure.Services;

public sealed class ProjectForecastExportService : IProjectForecastExportService
{
    public Task ExportToExcelAsync(ProjectForecastReportDto report, string filePath)
    {
        return Task.Run(() => BuildExcel(report, filePath));
    }

    private static void BuildExcel(ProjectForecastReportDto report, string filePath)
    {
        using var wb = new XLWorkbook();

        AddScenariosSheet(wb, report);
        AddVolumesSheet(wb, report);
        AddTop3Sheet(wb, report);

        wb.SaveAs(filePath);
    }

    // ─── Лист 1: Сценарии трудозатрат ────────────────────────────────────────

    private static void AddScenariosSheet(IXLWorkbook wb, ProjectForecastReportDto report)
    {
        var ws = wb.Worksheets.Add("Сценарии");

        ws.Cell(1, 1).Value = $"Прогноз трудозатрат: {report.ProjectName} ({report.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 5).Merge();

        var headers = new[] { "Сценарий", "Z-оценка", "Трудозатраты, ч", "% от базового", "Интерпретация" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        foreach (var s in report.Scenarios)
        {
            ws.Cell(row, 1).Value = s.ScenarioName;
            ws.Cell(row, 2).Value = s.ZScore;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Value = s.TotalLaborHours;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        // Интерпретации
        row++;
        ws.Cell(row, 1).Value = "Интерпретация сценариев";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, 5).Merge();
        row++;

        var intHeaders = new[] { "Сценарий", "Трудозатраты, ч", "% от базового", "Интерпретация" };
        WriteHeaderRow(ws, row, intHeaders);
        row++;

        foreach (var i in report.Interpretations)
        {
            ws.Cell(row, 1).Value = i.ScenarioName;
            ws.Cell(row, 2).Value = i.LaborHours;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Value = i.PercentOfBase;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 4).Value = i.Interpretation;
            row++;
        }

        ws.Column(1).Width = 22;
        ws.Column(4).Width = 14;
        ws.Column(5).Width = 40;
        ws.Columns(2, 3).AdjustToContents();
        ws.SheetView.FreezeRows(2);
    }

    // ─── Лист 2: Плановые объёмы ─────────────────────────────────────────────

    private static void AddVolumesSheet(IXLWorkbook wb, ProjectForecastReportDto report)
    {
        var ws = wb.Worksheets.Add("Плановые объёмы");

        ws.Cell(1, 1).Value = $"Плановые объёмы: {report.ProjectName} ({report.VersionName})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 8).Merge();

        var allRows = report.Scenarios.FirstOrDefault()?.Disciplines
            ?? report.PlannedVolumes.Select(v => new ForecastIicVolumeRowDto(
                v.WorkDirectionId, v.ParentId, v.Name, v.UnitName,
                v.NormProductivity, v.NormEstimate, v.NormStandardDeviation,
                v.PlannedVolume, v.OptimisticHours, v.BaseHours, v.PessimisticHours,
                v.Range, v.HierarchyPath)).ToList();

        var headers = new[] { "Направление ИИС", "Ед. изм.", "Норм. произв.", "Плановый объём", "Базовый, ч", "Оптимист., ч", "Пессимист., ч", "Диапазон" };
        WriteHeaderRow(ws, 2, headers);

        var idToLevel = BuildLevelMap(report.PlannedVolumes);
        var row = 3;
        foreach (var v in report.PlannedVolumes)
        {
            var level = idToLevel.TryGetValue(v.WorkDirectionId, out var l) ? l : 0;
            var indent = new string(' ', level * 2);
            ws.Cell(row, 1).Value = indent + v.Name;
            ws.Cell(row, 2).Value = v.UnitName ?? string.Empty;
            SetDecimal(ws.Cell(row, 3), v.NormProductivity, "N4");
            SetDecimal(ws.Cell(row, 4), v.PlannedVolume > 0 ? v.PlannedVolume : null, "N2");
            SetDecimal(ws.Cell(row, 5), v.BaseHours > 0 ? v.BaseHours : null, "N2");
            SetDecimal(ws.Cell(row, 6), v.OptimisticHours > 0 ? v.OptimisticHours : null, "N2");
            SetDecimal(ws.Cell(row, 7), v.PessimisticHours > 0 ? v.PessimisticHours : null, "N2");
            ws.Cell(row, 8).Value = v.Range;

            if (level == 0)
                ws.Row(row).Style.Font.Bold = true;
            else if (row % 2 == 0)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

            row++;
        }

        var tableRange = ws.Range(2, 1, row - 1, headers.Length);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;

        ws.Column(1).Width = 45;
        ws.Column(2).Width = 12;
        ws.Column(8).Width = 18;
        for (var c = 3; c <= 7; c++) ws.Column(c).AdjustToContents();
        ws.SheetView.FreezeRows(2);
    }

    // ─── Лист 3: ТОП-3 дисциплин ─────────────────────────────────────────────

    private static void AddTop3Sheet(IXLWorkbook wb, ProjectForecastReportDto report)
    {
        var ws = wb.Worksheets.Add("ТОП дисциплин");

        ws.Cell(1, 1).Value = "ТОП дисциплин по трудозатратам";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Range(1, 1, 1, 3).Merge();

        var headers = new[] { "Место", "Дисциплина", "Базовые трудозатраты, ч" };
        WriteHeaderRow(ws, 2, headers);

        var row = 3;
        foreach (var t in report.Top3Disciplines)
        {
            ws.Cell(row, 1).Value = t.Rank;
            ws.Cell(row, 2).Value = t.DisciplineName;
            ws.Cell(row, 3).Value = t.BaseHours;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        ws.Column(1).Width = 8;
        ws.Column(2).Width = 45;
        ws.Column(3).AdjustToContents();
        ws.Range(2, 1, row - 1, 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(2, 1, row - 1, 3).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
    }

    // ─── Вспомогательные ─────────────────────────────────────────────────────

    private static void WriteHeaderRow(IXLWorksheet ws, int row, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.WrapText = true;
        }
    }

    private static void SetDecimal(IXLCell cell, decimal? value, string format)
    {
        if (!value.HasValue) return;
        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = format == "N4" ? "#,##0.0000" : "#,##0.00";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static Dictionary<int, int> BuildLevelMap(IReadOnlyList<ForecastIicVolumeRowDto> rows)
    {
        var parentMap = rows.ToDictionary(r => r.WorkDirectionId, r => r.ParentId);
        var levels = new Dictionary<int, int>();

        int GetLevel(int id)
        {
            if (levels.TryGetValue(id, out var cached)) return cached;
            if (!parentMap.TryGetValue(id, out var pid) || pid is null)
            {
                levels[id] = 0;
                return 0;
            }
            var level = GetLevel(pid.Value) + 1;
            levels[id] = level;
            return level;
        }

        foreach (var r in rows) GetLevel(r.WorkDirectionId);
        return levels;
    }
}
