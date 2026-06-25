using ClosedXML.Excel;
using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EngineeringPerformance.Infrastructure.Export;

/// <summary>
/// Экспорт каталога проектов и разрешение versionId для экспорта одного проекта.
/// </summary>
public sealed class ProjectsCatalogExportService : IProjectsCatalogExportService
{
    private readonly AppDbContext _context;

    public ProjectsCatalogExportService(AppDbContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportAllToExcelAsync(CancellationToken cancellationToken = default)
    {
        var projects = await LoadActiveProjectsAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Projects");
        sheet.Cell(1, 1).Value = "Code";
        sheet.Cell(1, 2).Value = "Name";
        sheet.Cell(1, 3).Value = "Customer";
        sheet.Cell(1, 4).Value = "Status";

        var row = 2;
        foreach (var project in projects)
        {
            sheet.Cell(row, 1).Value = project.Code;
            sheet.Cell(row, 2).Value = project.Name;
            sheet.Cell(row, 3).Value = project.Customer ?? string.Empty;
            sheet.Cell(row, 4).Value = project.WorkStatus ?? string.Empty;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportAllToPdfAsync(CancellationToken cancellationToken = default)
    {
        var projects = await LoadActiveProjectsAsync(cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text("Engineering Performance — Projects").SemiBold().FontSize(16);
                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    foreach (var project in projects)
                    {
                        column.Item().Text($"{project.Code} — {project.Name}");
                    }
                });
                page.Footer().AlignCenter().Text($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        });

        return document.GeneratePdf();
    }

    public async Task<int?> GetFirstVersionIdAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var versionId = await _context.ProjectAnalysisVersions
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId)
            .OrderBy(v => v.Id)
            .Select(v => v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return versionId == 0 ? null : versionId;
    }

    private async Task<IReadOnlyList<ProjectRow>> LoadActiveProjectsAsync(CancellationToken cancellationToken) =>
        await _context.Projects
            .AsNoTracking()
            .Where(p => !p.IsArchived)
            .OrderBy(p => p.Code)
            .Select(p => new ProjectRow(p.Code, p.Name, p.Customer, p.WorkStatus))
            .ToListAsync(cancellationToken);

    private sealed record ProjectRow(string Code, string Name, string? Customer, string? WorkStatus);
}
