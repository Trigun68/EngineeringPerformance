namespace EngineeringPerformance.Api.Security;

/// <summary>
/// Серверная валидация загрузки Excel (расширение, MIME, magic-bytes, размер, имя файла).
/// </summary>
public static class ExcelUploadValidator
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xlsx" };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/octet-stream",
    };

    public static bool TryValidate(IFormFile file, out string error)
    {
        error = string.Empty;

        if (file.Length == 0)
        {
            error = "Empty file.";
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            error = $"File exceeds maximum size of {MaxFileSizeBytes / (1024 * 1024)} MB.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            error = "Only .xlsx files are allowed.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
        {
            error = "Invalid content type for Excel upload.";
            return false;
        }

        if (!HasXlsxSignature(file))
        {
            error = "File content does not match XLSX format.";
            return false;
        }

        return true;
    }

    public static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            return "upload.xlsx";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(safeName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return cleaned.Length > 200 ? cleaned[..200] : cleaned;
    }

    private static bool HasXlsxSignature(IFormFile file)
    {
        Span<byte> header = stackalloc byte[4];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        // XLSX — ZIP-контейнер (PK\x03\x04)
        return read >= 2 && header[0] == 0x50 && header[1] == 0x4B;
    }
}
