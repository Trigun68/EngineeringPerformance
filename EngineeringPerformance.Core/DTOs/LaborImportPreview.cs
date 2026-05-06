namespace EngineeringPerformance.Core.DTOs;

/// <summary>
/// Результат предпросмотра импорта трудоёмкости: перечень «Типов документа»
/// из Excel-файла, которые отсутствуют в справочнике LaborIntensities.
/// Формируется до записи в БД, чтобы интерфейс мог запросить у пользователя
/// решение: добавлять ли отсутствующие позиции в каталог, импортировать
/// без добавления или отменить импорт.
/// </summary>
public sealed record LaborImportPreview(
    IReadOnlyList<string> UnmatchedDocumentTypes,
    int TotalDataRows);
