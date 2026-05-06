# -*- coding: utf-8 -*-
"""Dump structure of PEPM_metric Excel (sheet «Прогноз») for aligning UI with the workbook.

Usage:
  python read_pepm_sheet.py [path_to_xlsx]

Default path: OneDrive Desktop PEPM_metric_v.2+.xlsx (Russian «Рабочий стол»).

Output: _dev/pepm_prognoz_dump.txt (UTF-8) next to this script.
"""
from __future__ import annotations

import os
import sys

try:
    import openpyxl
except ImportError:
    print("Install: pip install openpyxl", file=sys.stderr)
    sys.exit(1)

_here = os.path.dirname(os.path.abspath(__file__))
_out = os.path.join(_here, "pepm_prognoz_dump.txt")

path = sys.argv[1] if len(sys.argv) > 1 else None
if not path:
    desktop = os.path.join(os.path.expanduser("~"), "OneDrive", "Рабочий стол", "PEPM_metric_v.2+.xlsx")
    path = desktop

lines: list[str] = []


def w(line: str = "") -> None:
    lines.append(line)


if not os.path.isfile(path):
    w(f"FILE NOT FOUND: {path}")
    w("Pass the full path to PEPM_metric_v.2+.xlsx as the first argument.")
    with open(_out, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print(_out)
    sys.exit(0)

wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
w(f"File: {path}")
w("Sheets:")
for i, name in enumerate(wb.sheetnames):
    w(f"  [{i}] {name}")

target = None
for name in wb.sheetnames:
    low = name.lower()
    if "рогн" in low or "prog" in low:
        target = name
        break

if target is None:
    w("")
    w("No sheet with 'прогноз' / 'prog' in name — using sheet index 2 if present.")
    if len(wb.sheetnames) > 2:
        target = wb.sheetnames[2]

if target:
    w("")
    w(f"--- Sheet: {target!r} (first 45 rows x 18 cols) ---")
    ws = wb[target]
    rows = list(ws.iter_rows(min_row=1, max_row=45, max_col=18, values_only=True))
    for r in rows:
        w(repr(r))
else:
    w("Could not select a sheet.")

with open(_out, "w", encoding="utf-8") as f:
    f.write("\n".join(lines))

print("Wrote", _out)
