namespace EngineeringPerformance.Application.Export;

public interface IDirectoryExportService
{
    byte[] ExportToExcel(DirectoryExportRequest request);
}
