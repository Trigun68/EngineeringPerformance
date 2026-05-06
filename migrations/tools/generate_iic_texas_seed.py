# -*- coding: utf-8 -*-
"""Генерирует фрагмент 003_SeedDataFromExcel.sql для IIC из листа «Техас» Excel.

Источник данных — единственный: лист «Техас» в «Копия ++++-1.xlsx».
Иерархия IIC строится по номеру «№» (например, 1.2.3 → родитель 1.2 → 1).
Нормы берутся из колонок: Производительность, Z-оценка, Cт. откл.
"""
from __future__ import annotations

import sys
from decimal import ROUND_HALF_UP, Decimal, InvalidOperation
from pathlib import Path

import pandas as pd

EXCEL = Path(__file__).resolve().parents[1].parent / ".src" / "Рабочие материалы v.3.xlsx"
SHEET = "Техас"
HEADER_ROW = 2  # 3-я строка Excel (1-based)

# Явный EffectiveFrom для записей, добавленных позже основного среза.
# Для остальных используется SYSUTCDATETIME() (момент применения миграции).
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


def main() -> int:
    out_path: Path | None = None
    if len(sys.argv) > 1 and sys.argv[1] == "-o" and len(sys.argv) > 2:
        out_path = Path(sys.argv[2])

    if not EXCEL.is_file():
        print(f"File not found: {EXCEL}", file=sys.stderr)
        return 1

    df = pd.read_excel(EXCEL, sheet_name=SHEET, header=HEADER_ROW)
    z_col = "Z-оценка"
    std_col = "Cт. откл."

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

    out: list[str] = []
    out.append("-- ============================================================================")
    out.append("-- Seed Data from Excel (003): IIC из листа «Техас» (Копия ++++-1.xlsx)")
    out.append("-- ----------------------------------------------------------------------------")
    out.append("-- Источник: .src/Копия ++++-1.xlsx, лист «Техас» (заголовки в 3-й строке).")
    out.append("-- Иерархия групп строится по номеру «№» (например, 1.2.3 → родитель 1.2 → 1).")
    out.append("-- Единицы — знаменатель из «Ед. изм. (метр)».")
    out.append("-- Нормы: Performance, Estimate (Z-оценка), StandardDeviation (Cт. откл.).")
    out.append("-- Идемпотентно: безопасно запускать повторно.")
    out.append("-- ============================================================================")
    out.append("")
    out.append("-- ----------------------------------------------------------------------------")
    out.append("-- 1. IIC Work Directions (иерархия по № из «Техаса»)")
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
    out.append("-- 2. IIC Work Direction Norms (Performance / Estimate=Z / StandardDeviation)")
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

    text = "\n".join(out)
    if out_path is not None:
        out_path.write_text(text, encoding="utf-8")
    else:
        sys.stdout.buffer.write(text.encode("utf-8"))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
