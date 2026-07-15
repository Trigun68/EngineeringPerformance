using EngineeringPerformance.Application.Export;
using EngineeringPerformance.Infrastructure.Export;

namespace EngineeringPerformance.Infrastructure.Export;

public sealed class DirectoryExportService : IDirectoryExportService
{
    public byte[] ExportToExcel(DirectoryExportRequest request)
    {
        if (request.Headers.Count == 0)
            throw new ArgumentException("At least one column header is required.", nameof(request));

        var columns = request.Headers
            .Select((header, index) => new ReferenceExcelExporter.ColumnDef(
                header,
                row => GetCellValue(row, index)))
            .ToList();

        var rowObjects = request.Rows.Cast<object>().ToList();
        return ReferenceExcelExporter.Export(request.TabName, columns, rowObjects);
    }

    private static string? GetCellValue(object row, int columnIndex)
    {
        if (row is not IReadOnlyList<string?> cells || columnIndex >= cells.Count)
            return null;

        return cells[columnIndex];
    }
}
