namespace EngineeringPerformance.Infrastructure.Import;

/// <summary>
/// Происхождение импортируемого файла. Записывается в итерацию импорта внутри той же
/// транзакции, что и сами данные, — иначе при сбое остаётся итерация без хэша и автора.
/// </summary>
internal sealed record ImportSourceMetadata(string FileName, string FileHash, int? UserId)
{
    /// <summary>Служебные вызовы без загрузки файла пользователем (миграции, тесты).</summary>
    public static ImportSourceMetadata Unknown(string filePath) =>
        new(Path.GetFileName(filePath), string.Empty, null);
}
