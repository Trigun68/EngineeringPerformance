#!/usr/bin/env python3
"""Extract single-sheet workbooks for project import from the nomenclature Excel file.

For «Список работ» sheets renames the equipment «ID по каталогу» header (col G)
so the backend importer picks the work catalog column (col K).
"""
from __future__ import annotations

import argparse
from pathlib import Path

from openpyxl import load_workbook


def extract_sheet(source: Path, sheet_name: str, output: Path, fix_works_header: bool) -> None:
    wb = load_workbook(source, data_only=False)
    if sheet_name not in wb.sheetnames:
        raise SystemExit(f"Sheet not found: {sheet_name!r}. Available: {wb.sheetnames}")

    for name in list(wb.sheetnames):
        if name != sheet_name:
            del wb[name]

    if fix_works_header:
        ws = wb[sheet_name]
        cell = ws.cell(row=3, column=7)
        if cell.value and "ID по каталогу" in str(cell.value):
            cell.value = "ID обор. каталога"

    wb.save(output)
    wb.close()
    print(f"Wrote {output} ({sheet_name})")


def main() -> None:
    parser = argparse.ArgumentParser(description="Extract import sheets from project Excel.")
    parser.add_argument("source", type=Path, help="Source .xlsx path")
    parser.add_argument("output_dir", type=Path, help="Output directory")
    args = parser.parse_args()

    args.output_dir.mkdir(parents=True, exist_ok=True)

    pairs = [
        ("Список работ_13.04.26", "works_13.04.26.xlsx", True),
        ("Список оборудования_13.04.26", "equipment_13.04.26.xlsx", False),
        ("Список работ_30.06.26", "works_30.06.26.xlsx", True),
        ("Список оборудования_30.06.26", "equipment_30.06.26.xlsx", False),
    ]

    for sheet, filename, fix in pairs:
        extract_sheet(args.source, sheet, args.output_dir / filename, fix)


if __name__ == "__main__":
    main()
