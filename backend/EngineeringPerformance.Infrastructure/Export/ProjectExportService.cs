using ClosedXML.Excel;
using EngineeringPerformance.Application.Analytics;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>
/// Экспорт версии проекта: данные из <see cref="IProjectsPageDataFetcher"/> (паритет с UI).
/// QuestPDF / ClosedXML — строго using для освобождения ресурсов.
/// </summary>
public sealed class ProjectExportService : IProjectExportService
{
    private readonly IProjectsPageDataFetcher _dataFetcher;

    public ProjectExportService(IProjectsPageDataFetcher dataFetcher)
    {
        _dataFetcher = dataFetcher;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportToPdfAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("Отчёт по проекту").SemiBold().FontSize(16);
                    col.Item().Text($"ProjectId: {projectId}, VersionId: {versionId}");
                    col.Item().Text($"Сформирован: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(c => RenderTableSection(c, "Прогноз — итого", new[]
                    {
                        "Направление", "План. объём", "База ч", "Опт.", "Пессим.",
                    }, data.Forecast.Summary.Select(r => new[]
                    {
                        r.Name,
                        r.PlannedVolume.ToString("N2"),
                        r.BaseHours.ToString("N2"),
                        r.OptimisticHours.ToString("N2"),
                        r.PessimisticHours.ToString("N2"),
                    })));

                    col.Item().Element(c => RenderTableSection(c, "Факт — итого", new[]
                    {
                        "Направление", "ФО", "Часы", "Z", "Интерпретация",
                    }, data.Fact.Performance.Select(r => new[]
                    {
                        r.WorkDirectionName,
                        r.FactPhysicalVolume?.ToString("N2") ?? "—",
                        r.FactLaborHours?.ToString("N2") ?? "—",
                        r.ZScore?.ToString("N4") ?? "—",
                        r.Interpretation?.ToRussian() ?? "—",
                    })));

                    var equipmentRows = data.Fact.Detail.Where(d => d.IsEquipment).ToList();
                    col.Item().Element(c => RenderTableSection(c, "Оборудование", new[]
                    {
                        "Объект", "Наименование", "Кол-во", "Часы",
                    }, equipmentRows.Select(r => new[]
                    {
                        r.FacilityName,
                        r.CatalogName,
                        r.Quantity.ToString("N2"),
                        r.FactLaborHours?.ToString("N2") ?? r.BaseHours?.ToString("N2") ?? "—",
                    })));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Engineering Performance · ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportToExcelAsync(
        int projectId,
        int versionId,
        int? compareVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var data = await _dataFetcher.GetProjectsPageDataAsync(
            projectId,
            versionId,
            compareVersionId,
            cancellationToken);

        using var workbook = new XLWorkbook();

        WriteForecastSheet(workbook, data);
        WriteFactSheet(workbook, data);

        var ppvRows = data.Fact.Detail.Where(d => !d.IsEquipment).ToList();
        WriteDetailSheet(workbook.Worksheets.Add("PPV"), ppvRows, includeForecastHours: false);

        var equipmentRows = data.Fact.Detail.Where(d => d.IsEquipment).ToList();
        WriteDetailSheet(workbook.Worksheets.Add("Equipment"), equipmentRows, includeForecastHours: true);

        var liSheet = workbook.Worksheets.Add("LI");
        liSheet.Cell(1, 1).Value = "Направление";
        liSheet.Cell(1, 2).Value = "ФО";
        liSheet.Cell(1, 3).Value = "Часы";
        liSheet.Cell(1, 4).Value = "Z";
        liSheet.Cell(1, 5).Value = "Интерпретация";
        var liRow = 2;
        foreach (var row in data.Fact.Performance)
        {
            liSheet.Cell(liRow, 1).Value = row.WorkDirectionName;
            liSheet.Cell(liRow, 2).Value = row.FactPhysicalVolume;
            liSheet.Cell(liRow, 3).Value = row.FactLaborHours;
            liSheet.Cell(liRow, 4).Value = row.ZScore;
            liSheet.Cell(liRow, 5).Value = row.Interpretation?.ToRussian() ?? string.Empty;
            liRow++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteForecastSheet(XLWorkbook workbook, ProjectsPageDataDto data)
    {
        var sheet = workbook.Worksheets.Add("Forecast");
        sheet.Cell(1, 1).Value = "Направление";
        sheet.Cell(1, 2).Value = "План. объём";
        sheet.Cell(1, 3).Value = "База ч";
        sheet.Cell(1, 4).Value = "Опт.";
        sheet.Cell(1, 5).Value = "Пессим.";
        var row = 2;
        foreach (var item in data.Forecast.Summary)
        {
            sheet.Cell(row, 1).Value = item.Name;
            sheet.Cell(row, 2).Value = item.PlannedVolume;
            sheet.Cell(row, 3).Value = item.BaseHours;
            sheet.Cell(row, 4).Value = item.OptimisticHours;
            sheet.Cell(row, 5).Value = item.PessimisticHours;
            row++;
        }
    }

    private static void WriteFactSheet(XLWorkbook workbook, ProjectsPageDataDto data)
    {
        var sheet = workbook.Worksheets.Add("Fact");
        sheet.Cell(1, 1).Value = "Направление";
        sheet.Cell(1, 2).Value = "ФО";
        sheet.Cell(1, 3).Value = "Часы";
        sheet.Cell(1, 4).Value = "Z";
        var row = 2;
        foreach (var item in data.Fact.Performance)
        {
            sheet.Cell(row, 1).Value = item.WorkDirectionName;
            sheet.Cell(row, 2).Value = item.FactPhysicalVolume;
            sheet.Cell(row, 3).Value = item.FactLaborHours;
            sheet.Cell(row, 4).Value = item.ZScore;
            row++;
        }
    }

    private static void WriteDetailSheet(
        IXLWorksheet sheet,
        IReadOnlyList<ProjectsPageDetailRowDto> rows,
        bool includeForecastHours)
    {
        sheet.Cell(1, 1).Value = "Код объекта";
        sheet.Cell(1, 2).Value = "Объект";
        sheet.Cell(1, 3).Value = "Шифр";
        sheet.Cell(1, 4).Value = "Наименование";
        sheet.Cell(1, 5).Value = "Кол-во";
        sheet.Cell(1, 6).Value = includeForecastHours ? "База ч" : "Факт часы";
        var rowIndex = 2;
        foreach (var row in rows)
        {
            sheet.Cell(rowIndex, 1).Value = row.FacilityCode;
            sheet.Cell(rowIndex, 2).Value = row.FacilityName;
            sheet.Cell(rowIndex, 3).Value = row.CatalogCode;
            sheet.Cell(rowIndex, 4).Value = row.CatalogName;
            sheet.Cell(rowIndex, 5).Value = row.Quantity;
            sheet.Cell(rowIndex, 6).Value = includeForecastHours
                ? row.BaseHours
                : row.FactLaborHours;
            rowIndex++;
        }
    }

    private static void RenderTableSection(
        IContainer container,
        string title,
        string[] headers,
        IEnumerable<string[]> rows)
    {
        container.Column(col =>
        {
            col.Item().Text(title).SemiBold().FontSize(11);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var i = 0; i < headers.Length; i++)
                        columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    foreach (var h in headers)
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).SemiBold();
                });

                foreach (var row in rows)
                {
                    foreach (var cell in row)
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(cell);
                }
            });
        });
    }
}
