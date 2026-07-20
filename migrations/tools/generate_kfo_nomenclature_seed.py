# -*- coding: utf-8 -*-
"""Генерирует seed/refresh SQL для PhysicalVolumesKfo из листа «Спр_Номенклатура ФО».

Иерархия: корень «0» → группы «KQ.xx» → работы «KQ.xx.yyy».
Связь с группами физ. объёмов (IIC): столбец «Классификатор КИМ» → Code «IIC-…».
"""
from __future__ import annotations

import argparse
import re
import sys
from decimal import ROUND_HALF_UP, Decimal, InvalidOperation
from pathlib import Path

import pandas as pd

DEFAULT_EXCEL = (
    Path(__file__).resolve().parents[1].parent / ".src" / "_Расчет_трудоемкости_2_версии.xl.xlsx"
)
SHEET = "Спр_Номенклатура ФО"
HEADER_ROW = 1  # 2-я строка Excel — заголовки
ROOT_CODE = "0"
ROOT_NAME = "Справочник Новый горизонт"

LEAF_CODE_RE = re.compile(r"^KQ\.\d+\.\d+$")
GROUP_CODE_RE = re.compile(r"^KQ\.\d+$")
KIM_CODE_RE = re.compile(r"^(IIC(?:-\d+)+)")


def sql_str(s: str) -> str:
    return "N'" + s.replace("'", "''") + "'"


def norm_code(v: object) -> str:
    if pd.isna(v):
        return ""
    s = str(v).strip()
    if s.endswith(".0") and s[:-2].replace(".", "").isdigit():
        s = s[:-2]
    if s in ("0", "0.0"):
        return ""
    return s


def parse_kim(raw: object) -> str | None:
    if pd.isna(raw):
        return None
    s = str(raw).strip()
    if s in ("", "-", "—"):
        return None
    m = KIM_CODE_RE.match(s)
    return m.group(1) if m else None


def kim_to_sql(kim: str | None) -> str:
    if not kim:
        return "NULL"
    return f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(kim)})"


def parse_coefficient(raw: object) -> str:
    if raw is None or (isinstance(raw, float) and pd.isna(raw)):
        return "1"
    if isinstance(raw, (int, float)):
        d = Decimal(str(raw))
    else:
        t = str(raw).strip().replace(",", ".")
        if t in ("", "-", "—"):
            return "1"
        try:
            d = Decimal(t)
        except InvalidOperation:
            return "1"
    q = d.quantize(Decimal("0.0001"), rounding=ROUND_HALF_UP)
    return str(q)


def truthy_bool(v: object) -> bool:
    if pd.isna(v):
        return False
    if isinstance(v, bool):
        return v
    if isinstance(v, (int, float)):
        return bool(int(v))
    t = str(v).strip().lower()
    return t in ("1", "true", "да", "yes")


def truncate(s: str, max_len: int) -> str:
    return s if len(s) <= max_len else s[:max_len]


def parent_for_group(code: str) -> str | None:
    if code == ROOT_CODE:
        return None
    if GROUP_CODE_RE.match(code):
        return ROOT_CODE
    if LEAF_CODE_RE.match(code):
        parts = code.split(".")
        return f"{parts[0]}.{parts[1]}"
    return ROOT_CODE


def compute_depth(code: str, nodes: dict[str, dict]) -> int:
    depth = 0
    cur: str | None = code
    seen: set[str] = set()
    while cur is not None and cur in nodes:
        parent = nodes[cur]["parent_code"]
        if parent is None:
            break
        if cur in seen:
            return 9999
        seen.add(cur)
        depth += 1
        cur = parent
    return depth


def sort_insert_order(nodes: dict[str, dict]) -> list[str]:
    return sorted(nodes.keys(), key=lambda c: (compute_depth(c, nodes), c))


def load_nodes(excel: Path) -> dict[str, dict]:
    df = pd.read_excel(excel, sheet_name=SHEET, header=HEADER_ROW)
    nodes: dict[str, dict] = {}
    groups: dict[str, str] = {}

    for _, row in df.iterrows():
        code = norm_code(row["Код КФО"])
        if not code or not code.startswith("KQ."):
            continue

        group_code = norm_code(row["Код групы КФО"])
        group_name_raw = row["Наименование групы КФО"]
        if group_code and group_code.startswith("KQ.") and GROUP_CODE_RE.match(group_code):
            gn = "" if pd.isna(group_name_raw) else str(group_name_raw).strip()
            if gn and gn != "0":
                groups.setdefault(group_code, gn)

        if not LEAF_CODE_RE.match(code):
            continue

        kim = parse_kim(row["Классификатор КИМ"])
        unit = "" if pd.isna(row["Ед. изм."]) else str(row["Ед. изм."]).strip()
        name = "" if pd.isna(row["Наименование КФО"]) else str(row["Наименование КФО"]).strip()
        desc = "" if pd.isna(row["Описание БЭР24"]) else str(row["Описание БЭР24"]).strip()
        coeff = parse_coefficient(row["Коэффициент"])
        is_assessment = truthy_bool(row["Оценка"])
        is_archived = truthy_bool(row["Архив"])

        if code in nodes:
            n = nodes[code]
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
            "parent_code": group_code if group_code else parent_for_group(code),
            "name": truncate(name or code, 500),
            "description": truncate(desc, 4000),
            "kim": kim,
            "unit": truncate(unit, 50),
            "coefficient": coeff,
            "is_assessment": is_assessment,
            "is_archived": is_archived,
        }

    for group_code, group_name in sorted(groups.items()):
        if group_code in nodes:
            continue
        nodes[group_code] = {
            "parent_code": parent_for_group(group_code),
            "name": truncate(group_name or group_code, 500),
            "description": "",
            "kim": None,
            "unit": "",
            "coefficient": "1",
            "is_assessment": False,
            "is_archived": False,
        }

    # KIM на уровне группы — из первой строки листа с этим кодом группы
    for _, row in df.iterrows():
        group_code = norm_code(row["Код групы КФО"])
        if not group_code or group_code not in nodes:
            continue
        kim = parse_kim(row["Классификатор КИМ"])
        if kim and nodes[group_code]["kim"] is None:
            nodes[group_code]["kim"] = kim

    nodes[ROOT_CODE] = {
        "parent_code": None,
        "name": ROOT_NAME,
        "description": "",
        "kim": None,
        "unit": "",
        "coefficient": "1",
        "is_assessment": False,
        "is_archived": False,
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


def node_insert_block(code: str, meta: dict, *, sort_order: int) -> list[str]:
    parent = meta["parent_code"]
    parent_sql = (
        "NULL"
        if parent is None
        else f"(SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = {sql_str(parent)})"
    )
    cls = kim_to_sql(meta["kim"])
    unit_name = meta["unit"]
    unit_sql = (
        f"(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
        if unit_name
        else "NULL"
    )
    ia = 1 if meta["is_assessment"] else 0
    arch = 1 if meta["is_archived"] else 0
    coeff = meta["coefficient"]

    lines = [
        f"IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = {sql_str(code)})",
        "BEGIN",
        "    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], "
        "[ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])",
        "    VALUES ("
        f"{sql_str(code)}, {sql_str(meta['name'])}, {sql_str(meta['description'])}, "
        f"{parent_sql}, {cls}, {unit_sql}, {ia}, {arch}, {coeff}, {sort_order}, SYSUTCDATETIME());",
        "END;",
        "GO",
        "",
    ]
    return lines


def node_update_block(code: str, meta: dict, *, sort_order: int) -> str:
    parent = meta["parent_code"]
    parent_sql = (
        "NULL"
        if parent is None
        else f"(SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = {sql_str(parent)})"
    )
    cls = kim_to_sql(meta["kim"])
    unit_name = meta["unit"]
    unit_sql = (
        f"(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
        if unit_name
        else "NULL"
    )
    ia = 1 if meta["is_assessment"] else 0
    arch = 1 if meta["is_archived"] else 0
    coeff = meta["coefficient"]
    return (
        f"UPDATE [PhysicalVolumesKfo] SET "
        f"[Name] = {sql_str(meta['name'])}, "
        f"[Description] = {sql_str(meta['description'])}, "
        f"[ParentId] = {parent_sql}, "
        f"[ClassifierId] = {cls}, "
        f"[UnitId] = {unit_sql}, "
        f"[IsAssessment] = {ia}, "
        f"[IsArchived] = {arch}, "
        f"[Coefficient] = {coeff}, "
        f"[SortOrder] = {sort_order}, "
        f"[ModifiedAt] = SYSUTCDATETIME() "
        f"WHERE [Code] = {sql_str(code)};"
    )


def emit_inherit_classifier() -> list[str]:
    return [
        "-- Наследование ClassifierId от родителя",
        "WHILE 1 = 1",
        "BEGIN",
        "    UPDATE pv",
        "    SET [ClassifierId] = p.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
        "    FROM [PhysicalVolumesKfo] pv",
        "    INNER JOIN [PhysicalVolumesKfo] p ON p.[Id] = pv.[ParentId]",
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
        "-- Номенклатура физических объёмов КФО — лист «Спр_Номенклатура ФО»",
        f"-- Источник: {excel.name}",
        "-- Иерархия: «0» → «KQ.xx» → «KQ.xx.yyy»; ClassifierId ← «Классификатор КИМ».",
        "-- ============================================================================",
        "",
    ]
    lines.extend(emit_units(units))
    for idx, code in enumerate(order):
        lines.extend(node_insert_block(code, nodes[code], sort_order=idx))
    lines.extend(emit_inherit_classifier())
    return "\n".join(lines) + "\n"


def emit_refresh(nodes: dict[str, dict], excel: Path) -> str:
    order = sort_insert_order(nodes)
    codes = list(order)
    units = collect_units(nodes)
    lines: list[str] = [
        "-- ============================================================================",
        "-- Refresh PhysicalVolumesKfo from «Спр_Номенклатура ФО» (replace by Code)",
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
        parent = meta["parent_code"]
        parent_sql = (
            "NULL"
            if parent is None
            else f"(SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = {sql_str(parent)})"
        )
        cls = kim_to_sql(meta["kim"])
        unit_name = meta["unit"]
        unit_sql = (
            f"(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
            if unit_name
            else "NULL"
        )
        ia = 1 if meta["is_assessment"] else 0
        arch = 1 if meta["is_archived"] else 0
        coeff = meta["coefficient"]
        lines.append(f"IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = {sql_str(code)})")
        lines.append("BEGIN")
        lines.append(
            "    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], "
            "[ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])"
        )
        lines.append(
            "    VALUES ("
            f"{sql_str(code)}, {sql_str(meta['name'])}, {sql_str(meta['description'])}, "
            f"{parent_sql}, {cls}, {unit_sql}, {ia}, {arch}, {coeff}, {idx}, SYSUTCDATETIME());"
        )
        lines.append("END;")
        lines.append("")

    lines.append("-- 2. Update existing nodes")
    for idx, code in enumerate(order):
        lines.append(node_update_block(code, nodes[code], sort_order=idx))
    lines.append("")

    lines.append("-- 3. Inherit ClassifierId from parent")
    lines.extend(
        [
            "WHILE 1 = 1",
            "BEGIN",
            "    UPDATE pv",
            "    SET [ClassifierId] = p.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()",
            "    FROM [PhysicalVolumesKfo] pv",
            "    INNER JOIN [PhysicalVolumesKfo] p ON p.[Id] = pv.[ParentId]",
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
        f"UPDATE [PhysicalVolumesKfo] SET [IsArchived] = 1, [ModifiedAt] = SYSUTCDATETIME() "
        f"WHERE [Code] NOT IN ({in_list});"
    )
    lines.append("")
    lines.append("COMMIT TRANSACTION;")
    lines.append("GO")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate PhysicalVolumesKfo SQL from Спр_Номенклатура ФО")
    parser.add_argument("--excel", type=Path, default=DEFAULT_EXCEL)
    parser.add_argument("-o", "--output", type=Path, required=True)
    parser.add_argument("--refresh", action="store_true", help="Emit UPSERT refresh script")
    args = parser.parse_args()

    if not args.excel.is_file():
        print(f"File not found: {args.excel}", file=sys.stderr)
        return 1

    nodes = load_nodes(args.excel)
    if len(nodes) <= 1:
        print("No KFO nodes parsed from Excel.", file=sys.stderr)
        return 1

    text = emit_refresh(nodes, args.excel) if args.refresh else emit_seed(nodes, args.excel)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(text, encoding="utf-8-sig")
    print(f"Wrote {len(nodes)} KFO nodes → {args.output}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
