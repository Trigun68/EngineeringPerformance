# -*- coding: utf-8 -*-
"""Генерирует seed/refresh SQL для PhysicalVolumesBer из листа «Спр_Номенклатура ФО».

Иерархия БЭР: сборники «01»..«17» (корни) → отделы → группы → расценки «01-01-001-01».
Связь с КФО: столбец «Код КФО» → [PhysicalVolumeKfoId].
Связь с группами физ. объёмов (IIC): «Классификатор КИМ» → [ClassifierId].
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import pandas as pd

from generate_kfo_nomenclature_seed import (
    HEADER_ROW,
    SHEET,
    kim_to_sql,
    parse_coefficient,
    parse_kim,
    sort_insert_order,
    sql_str,
    truncate,
    truthy_bool,
)

DEFAULT_EXCEL = (
    Path(__file__).resolve().parents[1].parent / ".src" / "_Расчет_трудоемкости_2_версии.xl.xlsx"
)


def norm_ber_code(v: object) -> str:
    if pd.isna(v):
        return ""
    s = str(v).strip()
    if s.endswith(".0") and s[:-2].replace(".", "").isdigit():
        s = s[:-2]
    if s in ("0", "0.0"):
        return ""
    return s


def norm_kfo_code(v: object) -> str:
    if pd.isna(v):
        return ""
    s = str(v).strip()
    if s in ("0", "0.0"):
        return ""
    return s


def parent_for_ber(code: str) -> str | None:
    parts = code.split("-")
    if len(parts) <= 1:
        return None
    return "-".join(parts[:-1])


def kfo_to_sql(kfo_code: str | None) -> str:
    if not kfo_code:
        return "NULL"
    return (
        f"(SELECT TOP 1 [Id] FROM [PhysicalVolumesKfo] "
        f"WHERE [Code] = {sql_str(kfo_code)} AND [IsArchived] = 0 "
        f"ORDER BY [Id])"
    )


def load_nodes(excel: Path) -> dict[str, dict]:
    df = pd.read_excel(excel, sheet_name=SHEET, header=HEADER_ROW)
    nodes: dict[str, dict] = {}

    for _, row in df.iterrows():
        code = norm_ber_code(row["Код БЭР24"])
        if not code:
            continue

        kfo_code = norm_kfo_code(row["Код КФО"])
        kim = parse_kim(row["Классификатор КИМ"])
        unit = "" if pd.isna(row["Ед. изм."]) else str(row["Ед. изм."]).strip()
        name = "" if pd.isna(row["Наименование БЭР24"]) else str(row["Наименование БЭР24"]).strip()
        desc = "" if pd.isna(row["Описание БЭР24"]) else str(row["Описание БЭР24"]).strip()
        coeff = parse_coefficient(row["Коэффициент"])
        is_assessment = truthy_bool(row["Оценка"])
        is_archived = truthy_bool(row["Архив"])

        if code in nodes:
            n = nodes[code]
            if not n["kfo_code"] and kfo_code:
                n["kfo_code"] = kfo_code
            if n["kim"] is None and kim:
                n["kim"] = kim
            if not n["unit"] and unit:
                n["unit"] = unit
            if not n["description"] and desc:
                n["description"] = desc
            n["is_assessment"] = n["is_assessment"] or is_assessment
            n["is_archived"] = n["is_archived"] or is_archived
            continue

        nodes[code] = {
            "parent_code": parent_for_ber(code),
            "name": truncate(name or code, 500),
            "description": truncate(desc, 4000),
            "kim": kim,
            "kfo_code": kfo_code,
            "unit": truncate(unit, 50),
            "coefficient": coeff,
            "is_assessment": is_assessment,
            "is_archived": is_archived,
        }

    return nodes


def collect_units(nodes: dict[str, dict]) -> set[str]:
    return {n["unit"] for n in nodes.values() if n["unit"]}


def emit_units(units: set[str]) -> list[str]:
    lines: list[str] = []
    if not units:
        return lines
    lines.append("-- Единицы измерения из листа «Спр_Номенклатура ФО»")
    for unit_name in sorted(units):
        lines.append(
            f"IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
        )
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])"
        )
        lines.append(f"    VALUES ({sql_str(unit_name)}, N'', 1000, 0, SYSUTCDATETIME());")
        lines.append("END;")
        lines.append("GO")
        lines.append("")
    return lines


def node_values(meta: dict, *, sort_order: int) -> tuple[str, str, str, str, str, str, str, str, str]:
    parent = meta["parent_code"]
    parent_sql = (
        "NULL"
        if parent is None
        else f"(SELECT [Id] FROM [PhysicalVolumesBer] WHERE [Code] = {sql_str(parent)})"
    )
    cls = kim_to_sql(meta["kim"])
    kfo = kfo_to_sql(meta["kfo_code"])
    unit_name = meta["unit"]
    unit_sql = (
        f"(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
        if unit_name
        else "NULL"
    )
    ia = 1 if meta["is_assessment"] else 0
    arch = 1 if meta["is_archived"] else 0
    coeff = meta["coefficient"]
    return parent_sql, cls, kfo, unit_sql, str(ia), str(arch), coeff, str(sort_order), meta["name"]


def emit_post_link_sql() -> list[str]:
    return [
        "-- ClassifierId: наследование от родителя в дереве БЭР",
        "WHILE 1 = 1",
        "BEGIN",
        "    UPDATE pv",
        "    SET [ClassifierId] = p.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
        "    FROM [PhysicalVolumesBer] pv",
        "    INNER JOIN [PhysicalVolumesBer] p ON p.[Id] = pv.[ParentId]",
        "    WHERE pv.[ClassifierId] IS NULL",
        "      AND p.[ClassifierId] IS NOT NULL;",
        "",
        "    IF @@ROWCOUNT = 0",
        "        BREAK;",
        "END;",
        "",
        "-- ClassifierId: из связанной номенклатуры КФО",
        "UPDATE ber",
        "SET [ClassifierId] = kfo.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
        "FROM [PhysicalVolumesBer] ber",
        "INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Id] = ber.[PhysicalVolumeKfoId]",
        "WHERE ber.[ClassifierId] IS NULL",
        "  AND kfo.[ClassifierId] IS NOT NULL;",
        "",
        "-- Повторное наследование ClassifierId от родителя БЭР",
        "WHILE 1 = 1",
        "BEGIN",
        "    UPDATE pv",
        "    SET [ClassifierId] = p.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
        "    FROM [PhysicalVolumesBer] pv",
        "    INNER JOIN [PhysicalVolumesBer] p ON p.[Id] = pv.[ParentId]",
        "    WHERE pv.[ClassifierId] IS NULL",
        "      AND p.[ClassifierId] IS NOT NULL;",
        "",
        "    IF @@ROWCOUNT = 0",
        "        BREAK;",
        "END;",
        "GO",
        "",
    ]


def emit_seed(nodes: dict[str, dict], excel: Path) -> str:
    order = sort_insert_order(nodes)
    units = collect_units(nodes)
    lines: list[str] = [
        "-- ============================================================================",
        "-- Номенклатура физических объёмов БЭР — лист «Спр_Номенклатура ФО»",
        f"-- Источник: {excel.name}",
        "-- PhysicalVolumeKfoId ← «Код КФО»; ClassifierId ← «Классификатор КИМ».",
        "-- ============================================================================",
        "",
    ]
    lines.extend(emit_units(units))

    for idx, code in enumerate(order):
        meta = nodes[code]
        parent_sql, cls, kfo, unit_sql, ia, arch, coeff, _, _ = node_values(meta, sort_order=idx)
        lines.append(f"IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesBer] WHERE [Code] = {sql_str(code)})")
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [PhysicalVolumesBer] ([Code], [Name], [Description], [ParentId], "
            "[ClassifierId], [PhysicalVolumeKfoId], [UnitId], [IsAssessment], [IsArchived], "
            "[Coefficient], [SortOrder], [CreatedAt])"
        )
        lines.append(
            "    VALUES ("
            f"{sql_str(code)}, {sql_str(meta['name'])}, {sql_str(meta['description'])}, "
            f"{parent_sql}, {cls}, {kfo}, {unit_sql}, {ia}, {arch}, {coeff}, {idx}, SYSUTCDATETIME());"
        )
        lines.append("END;")
        lines.append("GO")
        lines.append("")

    lines.extend(emit_post_link_sql())
    return "\n".join(lines) + "\n"


def emit_refresh(nodes: dict[str, dict], excel: Path) -> str:
    order = sort_insert_order(nodes)
    codes = list(order)
    units = collect_units(nodes)
    lines: list[str] = [
        "-- ============================================================================",
        "-- Refresh PhysicalVolumesBer from «Спр_Номенклатура ФО» (replace by Code)",
        f"-- Источник: {excel.name}",
        "-- ============================================================================",
        "SET NOCOUNT ON;",
        "BEGIN TRANSACTION;",
        "",
    ]

    for unit_name in sorted(units):
        lines.append(
            f"IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
        )
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])"
        )
        lines.append(f"    VALUES ({sql_str(unit_name)}, N'', 1000, 0, SYSUTCDATETIME());")
        lines.append("END;")
        lines.append("")

    lines.append("-- 1. Insert missing nodes")
    for idx, code in enumerate(order):
        meta = nodes[code]
        parent_sql, cls, kfo, unit_sql, ia, arch, coeff, _, _ = node_values(meta, sort_order=idx)
        lines.append(f"IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesBer] WHERE [Code] = {sql_str(code)})")
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [PhysicalVolumesBer] ([Code], [Name], [Description], [ParentId], "
            "[ClassifierId], [PhysicalVolumeKfoId], [UnitId], [IsAssessment], [IsArchived], "
            "[Coefficient], [SortOrder], [CreatedAt])"
        )
        lines.append(
            "    VALUES ("
            f"{sql_str(code)}, {sql_str(meta['name'])}, {sql_str(meta['description'])}, "
            f"{parent_sql}, {cls}, {kfo}, {unit_sql}, {ia}, {arch}, {coeff}, {idx}, SYSUTCDATETIME());"
        )
        lines.append("END;")
        lines.append("")

    lines.append("-- 2. Update existing nodes")
    for idx, code in enumerate(order):
        meta = nodes[code]
        parent_sql, cls, kfo, unit_sql, ia, arch, coeff, _, name = node_values(meta, sort_order=idx)
        lines.append(
            f"UPDATE [PhysicalVolumesBer] SET "
            f"[Name] = {sql_str(meta['name'])}, "
            f"[Description] = {sql_str(meta['description'])}, "
            f"[ParentId] = {parent_sql}, "
            f"[ClassifierId] = {cls}, "
            f"[PhysicalVolumeKfoId] = {kfo}, "
            f"[UnitId] = {unit_sql}, "
            f"[IsAssessment] = {ia}, "
            f"[IsArchived] = {arch}, "
            f"[Coefficient] = {coeff}, "
            f"[SortOrder] = {idx}, "
            f"[ModifiedAt] = SYSUTCDATETIME() "
            f"WHERE [Code] = {sql_str(code)};"
        )
    lines.append("")

    lines.append("-- 3. ClassifierId from parent / linked KFO")
    lines.extend(
        [
            "WHILE 1 = 1",
            "BEGIN",
            "    UPDATE pv",
            "    SET [ClassifierId] = p.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
            "    FROM [PhysicalVolumesBer] pv",
            "    INNER JOIN [PhysicalVolumesBer] p ON p.[Id] = pv.[ParentId]",
            "    WHERE pv.[ClassifierId] IS NULL",
            "      AND p.[ClassifierId] IS NOT NULL;",
            "",
            "    IF @@ROWCOUNT = 0",
            "        BREAK;",
            "END;",
            "",
            "UPDATE ber",
            "SET [ClassifierId] = kfo.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
            "FROM [PhysicalVolumesBer] ber",
            "INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Id] = ber.[PhysicalVolumeKfoId]",
            "WHERE ber.[ClassifierId] IS NULL",
            "  AND kfo.[ClassifierId] IS NOT NULL;",
            "",
            "WHILE 1 = 1",
            "BEGIN",
            "    UPDATE pv",
            "    SET [ClassifierId] = p.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
            "    FROM [PhysicalVolumesBer] pv",
            "    INNER JOIN [PhysicalVolumesBer] p ON p.[Id] = pv.[ParentId]",
            "    WHERE pv.[ClassifierId] IS NULL",
            "      AND p.[ClassifierId] IS NOT NULL;",
            "",
            "    IF @@ROWCOUNT = 0",
            "        BREAK;",
            "END;",
            "",
        ]
    )

    in_list = ", ".join(sql_str(c) for c in codes)
    lines.append("-- 4. Archive nodes removed from source")
    lines.append(
        f"UPDATE [PhysicalVolumesBer] SET [IsArchived] = 1, [ModifiedAt] = SYSUTCDATETIME() "
        f"WHERE [Code] NOT IN ({in_list});"
    )
    lines.append("")
    lines.append("COMMIT TRANSACTION;")
    lines.append("GO")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate PhysicalVolumesBer SQL from Спр_Номенклатура ФО")
    parser.add_argument("--excel", type=Path, default=DEFAULT_EXCEL)
    parser.add_argument("-o", "--output", type=Path, required=True)
    parser.add_argument("--refresh", action="store_true")
    args = parser.parse_args()

    if not args.excel.is_file():
        print(f"File not found: {args.excel}", file=sys.stderr)
        return 1

    nodes = load_nodes(args.excel)
    if not nodes:
        print("No BER nodes parsed from Excel.", file=sys.stderr)
        return 1

    text = emit_refresh(nodes, args.excel) if args.refresh else emit_seed(nodes, args.excel)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(text, encoding="utf-8-sig")
    print(f"Wrote {len(nodes)} BER nodes → {args.output}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
