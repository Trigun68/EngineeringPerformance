namespace EngineeringPerformance.Application.Registers;

/// <summary>
/// Выделение кода комплекта документов из исходной строки импорта (часть после первого '|').
/// Пусто / нет разделителя → карантин (DocSetId = null).
/// </summary>
public static class DocSetCodeParser
{
    public static string? Extract(string? documentationCode)
    {
        if (string.IsNullOrWhiteSpace(documentationCode))
            return null;

        var pipe = documentationCode.IndexOf('|');
        if (pipe < 0 || pipe >= documentationCode.Length - 1)
            return null;

        var code = documentationCode[(pipe + 1)..].Trim();
        return string.IsNullOrEmpty(code) ? null : code;
    }
}
