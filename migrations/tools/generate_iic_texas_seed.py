# -*- coding: utf-8 -*-
"""Генерирует 003_SeedDataFromExcel.sql для IIC из листа «Спр_Техас» Excel.

Иерархия IIC строится по номеру «№» (например, 1.2.3 → родитель 1.2 → 1).
Нормы: Производительность, EPM (Z-оценка), Cт. откл. (метр.).
"""
from __future__ import annotations

import argparse
import sys
from decimal import ROUND_HALF_UP, Decimal, InvalidOperation
from pathlib import Path

import pandas as pd

DEFAULT_EXCEL = (
    Path(__file__).resolve().parents[1].parent / ".src" / "_Расчет_трудоемкости_2_версии.xl.xlsx"
)
SHEET = "Спр_Техас"
HEADER_ROW = 3  # 4-я строка Excel (1-based), строка заголовков

EFFECTIVE_FROM_OVERRIDE: dict[str, str] = {
    "IIC-6-11": "2026-04-24",
    "IIC-6-12": "2026-04-24",
}


def parse_no(no: object) -> tuple[int, ...]:
    s = str(no).strip().rstrip(".")
    return tuple(int(p) for p in s.split(".") if p)


def parts_to_code(parts: tuple[int, ...]) -> str:
    return "IIC-" + "-".join(str(p) for p in parts)


def sql_str(s: str) -> str:
    return "N'" + s.replace("'", "''") + "'"


def sql_decimal(v: object) -> str:
    if v is None or (isinstance(v, float) and pd.isna(v)):
        return "NULL"
    if isinstance(v, str):
        t = v.strip()
        if t in ("", "-", "—"):
            return "NULL"
        try:
            d = Decimal(t.replace(",", "."))
        except InvalidOperation:
            return "NULL"
        q = d.quantize(Decimal("0.0001"), rounding=ROUND_HALF_UP)
        return str(q)
    try:
        d = Decimal(str(v))
    except InvalidOperation:
        return "NULL"
    q = d.quantize(Decimal("0.0001"), rounding=ROUND_HALF_UP)
    return str(q)


def pick_name(row: pd.Series) -> str:
    l4 = row.get("4 уровень")
    l3 = row.get("3 уровень")
    l2 = row.get("2 уровень")

    def ok(x) -> bool:
        if pd.isna(x):
            return False
        t = str(x).strip()
        return t not in ("", "(-)", "-", "—")

    if ok(l4):
        return str(l4).strip()
    if ok(l3):
        return str(l3).strip()
    if ok(l2):
        return str(l2).strip()
    return "—"


def metric_to_unit_name(metric: object) -> str | None:
    if pd.isna(metric):
        return None
    m = str(metric).strip().lower()
    if not m or m == "nan":
        return None
    if "/" in m:
        m = m.split("/", 1)[1].strip()
    mapping = {
        "м³": "м³",
        "м3": "м³",
        "ярд³": "ярд³",
        "шт": "шт",
        "шт.": "шт",
        "тн.": "т",
        "т": "т",
        "м": "м",
        "заделка": None,
        "точка": None,
        "свет. точка": None,
        "контур": None,
        "ед.": None,
        "п.фут": None,
    }
    return mapping.get(m, None)


def resolve_std_col(columns: list[str]) -> str:
    normalized = [(c, str(c).replace("\n", " ").strip().lower()) for c in columns]
    for c, n in normalized:
        if "откл" in n and "метр" in n and "лог" not in n:
            return c
    for c, n in normalized:
        if "откл" in n:
            return c
    raise KeyError("Standard deviation column not found")


def load_rows(excel: Path) -> list[dict]:
    df = pd.read_excel(excel, sheet_name=SHEET, header=HEADER_ROW)
    std_col = resolve_std_col(list(df.columns))
    z_col = "EPM" if "EPM" in df.columns else "Z-оценка"

    rows: list[dict] = []
    for i in range(len(df)):
        r = df.iloc[i]
        no_raw = r.get("№")
        if pd.isna(no_raw):
            continue
        parts = parse_no(no_raw)
        if not parts:
            continue
        code = parts_to_code(parts)
        parent_code = parts_to_code(parts[:-1]) if len(parts) > 1 else None
        desc = r.get("Описание")
        description = "" if pd.isna(desc) else str(desc).strip()
        if len(description) > 2000:
            description = description[:2000]
        name = pick_name(r)
        if len(name) > 500:
            name = name[:500]
        unit_name = metric_to_unit_name(r.get("Ед. изм. (метр)"))
        rows.append(
            {
                "sort_key": parts,
                "code": code,
                "parent_code": parent_code,
                "name": name,
                "description": description,
                "unit_sql": (
                    f"(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = {sql_str(unit_name)})"
                    if unit_name
                    else "NULL"
                ),
                "perf": r.get("Производительность"),
                "z": r.get(z_col),
                "std": r.get(std_col),
            }
        )

    rows.sort(key=lambda x: x["sort_key"])
    return rows


def emit_seed(rows: list[dict], excel: Path) -> list[str]:
    out: list[str] = []
    out.append("-- ============================================================================")
    out.append("-- Seed Data from Excel (003): IIC из листа «Спр_Техас»")
    out.append("-- ----------------------------------------------------------------------------")
    out.append(f"-- Источник: {excel.name}, лист «{SHEET}» (заголовки в строке {HEADER_ROW + 1}).")
    out.append("-- Иерархия групп строится по номеру «№» (например, 1.2.3 → родитель 1.2 → 1).")
    out.append("-- Единицы — знаменатель из «Ед. изм. (метр)».")
    out.append("-- Нормы: Performance, Estimate (EPM), StandardDeviation (Cт. откл. метр.).")
    out.append("-- Идемпотентно: безопасно запускать повторно.")
    out.append("-- ============================================================================")
    out.append("")
    out.append("-- ----------------------------------------------------------------------------")
    out.append("-- 1. IIC Work Directions (иерархия по № из «Спр_Техас»)")
    out.append("-- ----------------------------------------------------------------------------")
    for idx, item in enumerate(rows):
        code = item["code"]
        parent_sql = (
            f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(item['parent_code'])})"
            if item["parent_code"]
            else "NULL"
        )
        out.append(f"IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = {sql_str(code)})")
        out.append("BEGIN")
        out.append(
            "    INSERT INTO [IicWorkDirections] "
            "([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])"
        )
        out.append(
            "    VALUES ("
            f"{sql_str(code)}, {sql_str(item['name'])}, {sql_str(item['description'])}, "
            f"{parent_sql}, {item['unit_sql']}, {idx}, 0, SYSUTCDATETIME());"
        )
        out.append("END;")
        out.append("")

    out.append("-- ----------------------------------------------------------------------------")
    out.append("-- 2. IIC Work Direction Norms (Performance / Estimate=EPM / StandardDeviation)")
    out.append("-- ----------------------------------------------------------------------------")
    out.append(
        "DECLARE @ChelHourIdNorm INT = "
        "(SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'час');"
    )
    out.append("")

    for item in rows:
        code = item["code"]
        p = sql_decimal(item["perf"])
        z = sql_decimal(item["z"])
        s = sql_decimal(item["std"])
        eff = EFFECTIVE_FROM_OVERRIDE.get(code)
        eff_sql = f"CONVERT(datetime2, '{eff}T00:00:00', 126)" if eff else "SYSUTCDATETIME()"
        out.append(
            f"IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] "
            f"WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(code)}))"
        )
        out.append("BEGIN")
        out.append(
            "    INSERT INTO [IicWorkDirectionNorms] "
            "([WorkDirectionId], [Performance], [Estimate], "
            "[StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])"
        )
        out.append(
            "    VALUES ("
            f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(code)}), "
            f"{p}, {z}, {s}, @ChelHourIdNorm, {eff_sql}, SYSUTCDATETIME());"
        )
        out.append("END;")
        out.append("")

    return out


def emit_refresh(rows: list[dict], excel: Path) -> list[str]:
    """UPSERT для уже существующей БД (replace справочника групп и норм)."""
    codes = [item["code"] for item in rows]
    out: list[str] = []
    out.append("-- ============================================================================")
    out.append("-- Refresh IIC groups from «Спр_Техас» (replace existing rows by Code)")
    out.append(f"-- Источник: {excel.name}")
    out.append("-- ============================================================================")
    out.append("SET NOCOUNT ON;")
    out.append("BEGIN TRANSACTION;")
    out.append("")

    out.append("-- 1. Insert missing directions")
    for idx, item in enumerate(rows):
        code = item["code"]
        parent_sql = (
            f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(item['parent_code'])})"
            if item["parent_code"]
            else "NULL"
        )
        out.append(f"IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = {sql_str(code)})")
        out.append("BEGIN")
        out.append(
            "    INSERT INTO [IicWorkDirections] "
            "([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])"
        )
        out.append(
            "    VALUES ("
            f"{sql_str(code)}, {sql_str(item['name'])}, {sql_str(item['description'])}, "
            f"{parent_sql}, {item['unit_sql']}, {idx}, 0, SYSUTCDATETIME());"
        )
        out.append("END;")
        out.append("")

    out.append("-- 2. Update existing directions (Name, Description, UnitId, SortOrder)")
    for idx, item in enumerate(rows):
        code = item["code"]
        out.append(
            f"UPDATE [IicWorkDirections] SET "
            f"[Name] = {sql_str(item['name'])}, "
            f"[Description] = {sql_str(item['description'])}, "
            f"[UnitId] = {item['unit_sql']}, "
            f"[SortOrder] = {idx}, "
            f"[IsArchived] = 0, "
            f"[ModifiedAt] = SYSUTCDATETIME() "
            f"WHERE [Code] = {sql_str(code)};"
        )
    out.append("")

    out.append("-- 3. Fix ParentId after all rows exist")
    for item in rows:
        code = item["code"]
        parent_sql = (
            f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(item['parent_code'])})"
            if item["parent_code"]
            else "NULL"
        )
        out.append(
            f"UPDATE [IicWorkDirections] SET [ParentId] = {parent_sql} "
            f"WHERE [Code] = {sql_str(code)};"
        )
    out.append("")

    in_list = ", ".join(sql_str(c) for c in codes)
    out.append(f"-- 4. Archive directions removed from source")
    out.append(
        f"UPDATE [IicWorkDirections] SET [IsArchived] = 1, [ModifiedAt] = SYSUTCDATETIME() "
        f"WHERE [Code] LIKE N'IIC-%' AND [Code] NOT IN ({in_list});"
    )
    out.append("")

    out.append("DECLARE @ChelHourIdNorm INT = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'час');")
    out.append("")
    out.append("-- 5. Upsert norms (latest row per direction)")
    for item in rows:
        code = item["code"]
        p = sql_decimal(item["perf"])
        z = sql_decimal(item["z"])
        s = sql_decimal(item["std"])
        wid = f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(code)})"
        out.append(
            f"IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = {wid})"
        )
        out.append("BEGIN")
        out.append(
            f"    UPDATE [IicWorkDirectionNorms] SET "
            f"[Performance] = {p}, [Estimate] = {z}, [StandardDeviation] = {s}, "
            f"[ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = {wid};"
        )
        out.append("END")
        out.append("ELSE")
        out.append("BEGIN")
        out.append(
            "    INSERT INTO [IicWorkDirectionNorms] "
            "([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) "
            f"VALUES ({wid}, {p}, {z}, {s}, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());"
        )
        out.append("END;")
        out.append("")

    out.append("COMMIT TRANSACTION;")
    out.append("GO")
    return out


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate IIC seed SQL from Спр_Техас Excel sheet")
    parser.add_argument("--excel", type=Path, default=DEFAULT_EXCEL, help="Path to source xlsx")
    parser.add_argument(
        "-o", "--output", type=Path, help="Output SQL path (default: stdout)"
    )
    parser.add_argument(
        "--refresh",
        action="store_true",
        help="Emit UPSERT refresh script instead of idempotent seed (003)",
    )
    args = parser.parse_args()

    if not args.excel.is_file():
        print(f"File not found: {args.excel}", file=sys.stderr)
        return 1

    rows = load_rows(args.excel)
    if not rows:
        print("No IIC rows parsed from Excel.", file=sys.stderr)
        return 1

    text = "\n".join(
        emit_refresh(rows, args.excel) if args.refresh else emit_seed(rows, args.excel)
    )
    if args.output is not None:
        args.output.write_text(text, encoding="utf-8")
        print(f"Wrote {len(rows)} IIC rows → {args.output}", file=sys.stderr)
    else:
        sys.stdout.buffer.write(text.encode("utf-8"))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
