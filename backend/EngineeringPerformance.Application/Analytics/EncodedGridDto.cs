namespace EngineeringPerformance.Application.Analytics;

/// <summary>
/// Колоночное представление грида для передачи по сети (ISS-019).
/// Гриды «Детально» — это десятки тысяч строк из ~36 полей, где имена ключей JSON занимают
/// больше места, чем данные, а строковые значения берутся из справочников в полторы тысячи
/// значений. Формат убирает и то, и другое: имена полей едут один раз в <see cref="Columns"/>,
/// повторяющиеся строки — один раз в <see cref="Strings"/>, а в ячейке остаётся индекс.
/// Преобразование обратимое и не знает бизнес-смысла полей: клиент восстанавливает те же строки.
/// </summary>
/// <param name="Columns">Имена полей в camelCase, порядок совпадает с порядком ячеек в строке.</param>
/// <param name="Strings">
/// Словари значений для колонок, где интернирование выгодно. Ячейка такой колонки — индекс
/// в массиве (или null). Колонки, которых здесь нет, везут значение как есть.
/// </param>
/// <param name="Rows">Строки грида: массив ячеек в порядке <see cref="Columns"/>.</param>
public sealed record EncodedGridDto(
    IReadOnlyList<string> Columns,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Strings,
    IReadOnlyList<object?[]> Rows);
