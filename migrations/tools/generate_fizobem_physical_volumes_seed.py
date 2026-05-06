# -*- coding: utf-8 -*-
"""Генерирует migrations/data/005_PhysicalVolumes.sql из листа «Физ.объем» Excel.

Code в справочнике — это «EP Код» без дополнительных префиксов. Коды корней
блоков КФО и БЭР совпадают («01»..«17»), поэтому идентификатор строки
в пределах справочника — это пара (VolumeType, Code): IF NOT EXISTS и поиск
родителя выполняются с фильтром по VolumeTypeId.
"""
from __future__ import annotations

import sys
from pathlib import Path

import pandas as pd

EXCEL = Path(__file__).resolve().parents[1].parent / ".src" / "Копия ++++-1.xlsx"
SHEET = "Физ.объем"
HEADER_ROW = 3  # 0-based: строка 4 Excel — заголовки столбцов
BLOCK_B_START = 285  # со строки 285 начинается второй блок (БЭР24-1)

sys.path.insert(0, str(Path(__file__).resolve().parent))
from generate_iic_texas_seed import parse_no, parts_to_code  # noqa: E402


def texas_to_iic_code(raw: str) -> str | None:
    """Преобразует значение столбца «Техас» (напр. «1.2.1.») в Code IIC («IIC-1-2-1»)."""
    try:
        parts = parse_no(raw)
    except (TypeError, ValueError):
        return None
    if not parts:
        return None
    return parts_to_code(parts)


def sql_str(s: str) -> str:
    return "N'" + s.replace("'", "''") + "'"


def norm_ep_code(v: object) -> str:
    if pd.isna(v):
        return ""
    if isinstance(v, float) and v == int(v):
        return str(int(v))
    return str(v).strip()


def volume_type_sql(name: str) -> str:
    return f"(SELECT [Id] FROM [PhysicalVolumeTypes] WHERE [Name] = N'{name}')"


def parent_code_block_a(ep_code: str) -> str | None:
    """Иерархия КФО: «0» — корень; разделы «01»..«17» → «0»; «KQ.xx.*» → раздел «xx»."""
    c = ep_code.strip()
    if c == "0":
        return None
    if c.startswith("KQ."):
        parts = c.split(".")
        if len(parts) >= 2:
            return parts[1]
        return "0"
    if c.isdigit() or (len(c) <= 2 and c.isalnum()):
        return "0"
    return None


def parent_code_block_b(ep_code: str) -> str | None:
    """Иерархия БЭР: сборники «01»..«17» — корни; «01-01» → «01»; «01-01-001» → «01-01»; …"""
    raw = ep_code.strip()
    parts = raw.split("-")
    if len(parts) == 1:
        return None
    return "-".join(parts[:-1])


def truthy_berem(v: object) -> bool:
    if pd.isna(v):
        return False
    if isinstance(v, (int, float)):
        return bool(int(v)) if v == v else False  # noqa: PLR2004
    t = str(v).strip().lower()
    return t in ("1", "true", "да", "yes")


def compute_depth(key: tuple[str, str], nodes: dict[tuple[str, str], dict]) -> int:
    """Глубина узла в своём дереве (ветка определяется первым элементом ключа)."""
    d = 0
    cur: tuple[str, str] | None = key
    seen: set[tuple[str, str]] = set()
    while cur is not None and cur in nodes:
        parent_code = nodes[cur]["parent_code"]
        if parent_code is None:
            break
        if cur in seen:
            return 9999
        seen.add(cur)
        d += 1
        cur = (cur[0], parent_code)
    return d


def sort_insert_order(
    nodes: dict[tuple[str, str], dict],
) -> list[tuple[str, str]]:
    return sorted(nodes.keys(), key=lambda k: (compute_depth(k, nodes), k[0], k[1]))


def main() -> int:
    out_path = Path(__file__).resolve().parents[1] / "data" / "005_PhysicalVolumes.sql"
    if len(sys.argv) >= 3 and sys.argv[1] == "-o":
        out_path = Path(sys.argv[2])

    if not EXCEL.is_file():
        print(f"File not found: {EXCEL}", file=sys.stderr)
        return 1

    df = pd.read_excel(EXCEL, sheet_name=SHEET, header=HEADER_ROW)
    col_code = "EP Код"
    col_name = "EP Наименование"
    col_desc = "Описательная часть"
    col_kind = "Вид справочника"
    col_berem = "Берем"
    col_texas = "Техас"
    col_unit = "ЕР Ед. изм."

    nodes: dict[tuple[str, str], dict] = {}
    units: set[str] = set()

    for i in range(len(df)):
        raw_code = norm_ep_code(df.iloc[i][col_code])
        if not raw_code:
            continue

        kind = df.iloc[i][col_kind]
        if pd.isna(kind):
            vol = "КФО"
        else:
            ks = str(kind).strip()
            if ks.startswith("БЭР") or ks.upper().startswith("BER"):
                vol = "БЭР"
            else:
                vol = "КФО"

        key = (vol, raw_code)

        texas = df.iloc[i][col_texas]
        classifier_sql = "NULL"
        if pd.notna(texas) and str(texas).strip() not in ("", "-", "—"):
            iic = texas_to_iic_code(str(texas).strip())
            if iic:
                classifier_sql = f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(iic)})"

        unit_raw = df.iloc[i][col_unit]
        unit_name = "" if pd.isna(unit_raw) else str(unit_raw).strip()
        if len(unit_name) > 50:
            unit_name = unit_name[:50]
        if unit_name:
            units.add(unit_name)

        if key in nodes:
            if nodes[key]["classifier_sql"] == "NULL" and classifier_sql != "NULL":
                nodes[key]["classifier_sql"] = classifier_sql
            if not nodes[key]["unit_name"] and unit_name:
                nodes[key]["unit_name"] = unit_name
            continue

        name = df.iloc[i][col_name]
        name_s = "" if pd.isna(name) else str(name).strip()
        if len(name_s) > 500:
            name_s = name_s[:500]
        desc = df.iloc[i][col_desc]
        desc_s = "" if pd.isna(desc) else str(desc).strip()
        if len(desc_s) > 4000:
            desc_s = desc_s[:4000]

        if i < BLOCK_B_START:
            parent = parent_code_block_a(raw_code)
        else:
            parent = parent_code_block_b(raw_code)

        is_assessment = 1 if truthy_berem(df.iloc[i][col_berem]) else 0

        nodes[key] = {
            "parent_code": parent,
            "name": name_s or raw_code,
            "description": desc_s,
            "volume": vol,
            "classifier_sql": classifier_sql,
            "is_assessment": is_assessment,
            "unit_name": unit_name,
        }

    order = sort_insert_order(nodes)

    lines: list[str] = [
        "-- ============================================================================",
        "-- Номенклатура физических объёмов — лист «Физ.объем», файл «Копия ++++-1.xlsx»",
        "-- Две независимые ветки дерева (без общего корня): КФО и БЭР.",
        "--   • КФО: корень «0» (Справочник Новый горизонт) → разделы «01»..«17» → работы «KQ.xx.YYY».",
        "--   • БЭР: 17 корней «01»..«17» → отделы «01-01».. → группы → расценки «01-01-001-01».",
        "-- Code = «EP Код» из Excel. Уникальность обеспечивается парой (VolumeTypeId, Code),",
        "-- поэтому все IF NOT EXISTS и поиски родителя фильтруют по VolumeTypeId.",
        "-- Оценка: столбец «Берем» (=1 → да, 0/пусто/иное → нет) → IsAssessment.",
        "-- Связь со справочником «Группы физ. объёмов (IIC)»: [ClassifierId] → [IicWorkDirections].[Id]",
        "--   (FK FK_PhysicalVolumes_IicWorkDirections_ClassifierId). Столбец «Техас» → Code «IIC-…».",
        "--   При дубликатах EP Код в листе берётся первая непустая «Техас».",
        "-- Единица измерения: столбец «ЕР Ед. изм.» → [UnitId] (FK на UnitsOfMeasurement).",
        "--   Все встретившиеся в листе значения добавляются в UnitsOfMeasurement идемпотентно.",
        "-- После вставок: наследование ClassifierId от предка, если у узла NULL, а у родителя задано.",
        "-- Порядок вставок: от корня к листьям. Идемпотентно по (VolumeTypeId, Code).",
        "-- ============================================================================",
        "IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PhysicalVolumes]') AND name = N'Description' AND max_length < 8000)",
        "BEGIN",
        "    ALTER TABLE [PhysicalVolumes] ALTER COLUMN [Description] nvarchar(4000) NOT NULL;",
        "END;",
        "",
    ]

    if units:
        lines.append("-- ----------------------------------------------------------------------------")
        lines.append("-- Единицы измерения из листа «Физ.объем» (idempotent INSERTs)")
        lines.append("-- ----------------------------------------------------------------------------")
        for unit_name in sorted(units):
            lines.append(
                f"IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
            )
            lines.append("BEGIN")
            lines.append(
                "    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])"
            )
            lines.append(
                f"    VALUES ({sql_str(unit_name)}, N'', 1000, 0, SYSUTCDATETIME());"
            )
            lines.append("END;")
            lines.append("GO")
            lines.append("")

    for key in order:
        vol, code = key
        meta = nodes[key]
        parent = meta["parent_code"]
        vol_sql = volume_type_sql(meta["volume"])
        if parent is None:
            parent_sql = "NULL"
        else:
            parent_sql = (
                f"(SELECT [Id] FROM [PhysicalVolumes] "
                f"WHERE [Code] = {sql_str(parent)} AND [VolumeTypeId] = {vol_sql})"
            )

        cls = meta["classifier_sql"]
        ia = meta["is_assessment"]
        unit_name = meta["unit_name"]
        if unit_name:
            unit_sql = (
                f"(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
            )
        else:
            unit_sql = "NULL"

        lines.append(
            f"IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumes] "
            f"WHERE [Code] = {sql_str(code)} AND [VolumeTypeId] = {vol_sql})"
        )
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [PhysicalVolumes] ([Code], [Name], [Description], [VolumeTypeId], [ParentId], "
            "[ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [CreatedAt])"
        )
        lines.append(
            f"    VALUES ({sql_str(code)}, {sql_str(meta['name'])}, {sql_str(meta['description'])}, "
            f"{vol_sql}, {parent_sql}, {cls}, {unit_sql}, {ia}, 0, 1, SYSUTCDATETIME());"
        )
        lines.append("END;")
        lines.append("ELSE")
        lines.append("BEGIN")
        lines.append(
            "    -- Backfill UnitId на уже существующих строках (миграция добавила колонку позже)."
        )
        lines.append(
            f"    UPDATE [PhysicalVolumes] SET [UnitId] = {unit_sql} "
            f"WHERE [Code] = {sql_str(code)} AND [VolumeTypeId] = {vol_sql} "
            f"AND ([UnitId] IS NULL OR [UnitId] <> {unit_sql});"
        )
        lines.append("END;")
        lines.append("GO")
        lines.append("")

    lines.extend(
        [
            "-- ----------------------------------------------------------------------------",
            "-- Заполнение [ClassifierId] у потомков по справочнику ИИС (наследование от родителя)",
            "-- ----------------------------------------------------------------------------",
            "WHILE 1 = 1",
            "BEGIN",
            "    UPDATE pv",
            "    SET [ClassifierId] = p.[ClassifierId]",
            "    FROM [PhysicalVolumes] pv",
            "    INNER JOIN [PhysicalVolumes] p ON p.[Id] = pv.[ParentId]",
            "    WHERE pv.[ClassifierId] IS NULL",
            "      AND p.[ClassifierId] IS NOT NULL;",
            "",
            "    IF @@ROWCOUNT = 0",
            "        BREAK;",
            "END;",
            "GO",
            "",
        ]
    )

    text = "\n".join(lines) + "\n"
    out_path.parent.mkdir(parents=True, exist_ok=True)
    # utf-8-sig: BOM нужен, чтобы sqlcmd интерпретировал файл как UTF-8,
    # иначе N'БЭР' / N'КФО' читаются в cp1251 и LOOKUP в PhysicalVolumeTypes
    # возвращает NULL → INSERT падает «Cannot insert NULL into VolumeTypeId».
    out_path.write_text(text, encoding="utf-8-sig")
    print(f"Wrote {out_path} ({len(order)} nodes)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
