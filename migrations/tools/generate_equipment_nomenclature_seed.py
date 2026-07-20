# -*- coding: utf-8 -*-
"""Генерирует seed/refresh SQL для Equipments из листа «Спр_Оборудование».

Связь с группами физ. объёмов (IIC): «Классификатор КИМ» → [ClassifierId].
Уникальность строк: CatalogKeyHash = SHA-256(Name + NUL + UnitId).
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import pandas as pd

from generate_kfo_nomenclature_seed import (
    kim_to_sql,
    parse_coefficient,
    parse_kim,
    sql_str,
    truncate,
)

DEFAULT_EXCEL = (
    Path(__file__).resolve().parents[1].parent / ".src" / "_Расчет_трудоемкости_2_версии.xl.xlsx"
)
SHEET = "Спр_Оборудование"
HEADER_ROW = 1


def norm_id(v: object) -> str:
    if pd.isna(v):
        return ""
    if isinstance(v, float) and v == int(v):
        return str(int(v))
    return str(v).strip()


def pick_name(row: pd.Series) -> str:
    main = row.get("Наименование по каталогу")
    if pd.notna(main) and str(main).strip():
        return str(main).strip()
    short = row.get("Наименование краткое")
    if pd.notna(short) and str(short).strip():
        return str(short).strip()
    return ""


def build_description(row: pd.Series, name: str) -> str:
    parts: list[str] = []
    short = row.get("Наименование краткое")
    if pd.notna(short):
        s = str(short).strip()
        if s and s != name:
            parts.append(s)
    type_mark = row.get("Тип/Марка оборудования")
    if pd.notna(type_mark) and str(type_mark).strip():
        parts.append(f"Тип/марка: {str(type_mark).strip()}")
    catalog_id = norm_id(row.get("ID по каталогу"))
    if catalog_id:
        parts.append(f"ID по каталогу: {catalog_id}")
    full_id = norm_id(row.get("ID обор. полный"))
    if full_id and full_id != catalog_id:
        parts.append(f"ID обор. полный: {full_id}")
    return truncate("\n".join(parts), 4000)


def truthy_assessment(v: object) -> bool:
    if pd.isna(v):
        return False
    if isinstance(v, bool):
        return v
    t = str(v).strip().lower()
    return t in ("1", "true", "да", "yes")


def catalog_hash_sql(name: str, unit_name: str) -> str:
    if unit_name:
        unit_part = (
            f"COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] "
            f"WHERE [Name] = {sql_str(unit_name)}) AS nvarchar(20)), N'')"
        )
    else:
        unit_part = "N''"
    return (
        f"LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', "
        f"CONCAT({sql_str(name)}, CHAR(0), {unit_part})), 2))"
    )


def unit_sql(unit_name: str) -> str:
    if not unit_name:
        return "NULL"
    return f"(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"


def load_rows(excel: Path) -> list[dict]:
    df = pd.read_excel(excel, sheet_name=SHEET, header=HEADER_ROW)
    rows: dict[tuple[str, str], dict] = {}

    for _, row in df.iterrows():
        name = pick_name(row)
        if not name:
            continue
        name = truncate(name, 1000)
        unit = "" if pd.isna(row.get("Ед. изм.")) else str(row["Ед. изм."]).strip()
        unit = truncate(unit, 50)
        key = (name, unit)

        kim = parse_kim(row.get("Классификатор КИМ"))
        coeff = parse_coefficient(row.get("Коэффициент"))
        is_assessment = truthy_assessment(row.get("Оценка"))
        desc = build_description(row, name)

        if key in rows:
            existing = rows[key]
            if existing["kim"] is None and kim:
                existing["kim"] = kim
            if not existing["description"] and desc:
                existing["description"] = desc
            existing["is_assessment"] = existing["is_assessment"] or is_assessment
            continue

        rows[key] = {
            "name": name,
            "description": desc,
            "unit": unit,
            "kim": kim,
            "coefficient": coeff,
            "is_assessment": is_assessment,
        }

    return list(rows.values())


def collect_units(rows: list[dict]) -> set[str]:
    return {r["unit"] for r in rows if r["unit"]}


def emit_units(units: set[str]) -> list[str]:
    lines: list[str] = []
    if not units:
        return lines
    lines.append("-- Единицы измерения из листа «Спр_Оборудование»")
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


def emit_row_upsert(row: dict, *, sort_order: int, seed: bool) -> list[str]:
    name = row["name"]
    unit = row["unit"]
    desc = row["description"]
    cls = kim_to_sql(row["kim"])
    u_sql = unit_sql(unit)
    coeff = row["coefficient"]
    ia = 1 if row["is_assessment"] else 0
    hash_sql = catalog_hash_sql(name, unit)

    lines: list[str] = []
    if seed:
        lines.append(f"IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = {hash_sql})")
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], "
            "[Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])"
        )
        lines.append(
            "    VALUES ("
            f"{sql_str(name)}, {sql_str(desc)}, {cls}, {u_sql}, {coeff}, {ia}, "
            f"NULL, {sort_order}, 0, SYSUTCDATETIME(), {hash_sql});"
        )
        lines.append("END;")
        lines.append("ELSE")
        lines.append("BEGIN")
        lines.append(
            f"    UPDATE [Equipments] SET [Name] = {sql_str(name)}, [Description] = {sql_str(desc)}, "
            f"[ClassifierId] = {cls}, [UnitId] = {u_sql}, [Coefficient] = {coeff}, [IsAssessment] = {ia}, "
            f"[SortOrder] = {sort_order}, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() "
            f"WHERE [CatalogKeyHash] = {hash_sql};"
        )
        lines.append("END;")
        lines.append("GO")
    else:
        lines.append(f"IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = {hash_sql})")
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], "
            "[Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])"
        )
        lines.append(
            "    VALUES ("
            f"{sql_str(name)}, {sql_str(desc)}, {cls}, {u_sql}, {coeff}, {ia}, "
            f"NULL, {sort_order}, 0, SYSUTCDATETIME(), {hash_sql});"
        )
        lines.append("END;")
        lines.append("ELSE")
        lines.append("BEGIN")
        lines.append(
            f"    UPDATE [Equipments] SET [Name] = {sql_str(name)}, [Description] = {sql_str(desc)}, "
            f"[ClassifierId] = {cls}, [UnitId] = {u_sql}, [Coefficient] = {coeff}, [IsAssessment] = {ia}, "
            f"[SortOrder] = {sort_order}, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() "
            f"WHERE [CatalogKeyHash] = {hash_sql};"
        )
        lines.append("END;")
        lines.append("")
    return lines


def emit_seed(rows: list[dict], excel: Path) -> str:
    units = collect_units(rows)
    lines: list[str] = [
        "-- ============================================================================",
        "-- Справочник оборудования — лист «Спр_Оборудование»",
        f"-- Источник: {excel.name}",
        "-- ClassifierId ← «Классификатор КИМ»; уникальность по CatalogKeyHash (Name + UnitId).",
        "-- ============================================================================",
        "SET NOCOUNT ON;",
        "",
    ]
    lines.extend(emit_units(units))
    for idx, row in enumerate(rows):
        lines.extend(emit_row_upsert(row, sort_order=idx, seed=True))
        lines.append("")
    return "\n".join(lines) + "\n"


def emit_refresh(rows: list[dict], excel: Path) -> str:
    units = collect_units(rows)
    lines: list[str] = [
        "-- ============================================================================",
        "-- Refresh Equipments from «Спр_Оборудование» (replace by CatalogKeyHash)",
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

    lines.append("-- 1. Upsert catalog rows from source")
    for idx, row in enumerate(rows):
        lines.extend(emit_row_upsert(row, sort_order=idx, seed=False))

    lines.append("-- 2. Archive equipment removed from source")
    lines.append(
        "UPDATE [Equipments] SET [IsArchived] = 1, [ModifiedAt] = SYSUTCDATETIME() "
        "WHERE [IsArchived] = 0 AND [CatalogKeyHash] NOT IN ("
    )
    hash_exprs = [catalog_hash_sql(r["name"], r["unit"]) for r in rows]
    lines.append(",\n".join(hash_exprs))
    lines.append(");")
    lines.append("")
    lines.append("COMMIT TRANSACTION;")
    lines.append("GO")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate Equipments SQL from Спр_Оборудование")
    parser.add_argument("--excel", type=Path, default=DEFAULT_EXCEL)
    parser.add_argument("-o", "--output", type=Path, required=True)
    parser.add_argument("--refresh", action="store_true")
    args = parser.parse_args()

    if not args.excel.is_file():
        print(f"File not found: {args.excel}", file=sys.stderr)
        return 1

    rows = load_rows(args.excel)
    if not rows:
        print("No equipment rows parsed from Excel.", file=sys.stderr)
        return 1

    text = emit_refresh(rows, args.excel) if args.refresh else emit_seed(rows, args.excel)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(text, encoding="utf-8-sig")
    print(f"Wrote {len(rows)} equipment rows → {args.output}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
