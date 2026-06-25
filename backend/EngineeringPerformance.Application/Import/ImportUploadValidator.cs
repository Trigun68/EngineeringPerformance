namespace EngineeringPerformance.Application.Import;

/// <summary>
/// Серверная валидация файла импорта (расширение, MIME, размер).
/// </summary>
public static class ImportUploadValidator
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx" };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/octet-stream",
    };

    public static bool TryValidate(string fileName, long fileSizeBytes, string? contentType, out string error)
    {
        error = string.Empty;

        if (fileSizeBytes <= 0)
        {
            error = "Empty file.";
            return false;
        }

        if (fileSizeBytes > MaxFileSizeBytes)
        {
            error = $"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.";
            return false;
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            error = "Only .xlsx files are allowed.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(contentType) && !AllowedContentTypes.Contains(contentType))
        {
            error = "Invalid content type for Excel upload.";
            return false;
        }

        return true;
    }
}
