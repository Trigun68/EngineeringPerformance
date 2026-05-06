import openpyxl, json, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

path = r"C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\.src\Рабочие материалы v.3.xlsx"
wb = openpyxl.load_workbook(path, data_only=True)
ws = wb["Труд"]
print(f"dims: {ws.dimensions}, max_row={ws.max_row}, max_col={ws.max_column}")

rows = list(ws.iter_rows(values_only=True))
print("Header:", rows[0])
print("Sample rows 2-5:")
for r in rows[1:5]:
    print(r)

# Count non-empty rows
non_empty = [r for r in rows[1:] if r[0]]
print(f"Non-empty data rows: {len(non_empty)}")

# Output JSON
out = []
header = list(rows[0])
print("Header indices:", {i: h for i, h in enumerate(header)})
for r in non_empty:
    out.append(dict(zip(header, r)))

with open(r"C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\_dev\trud_data.json", "w", encoding="utf-8") as f:
    json.dump(out, f, ensure_ascii=False, indent=2, default=str)
print(f"Saved {len(out)} rows")

# unique values in some fields
for fld in ["Код ИСР", "Направление", "Код направления", "Тип документа", "Идентификатор типа документа", "Техас"]:
    vals = set(d.get(fld) for d in out)
    print(f"\n{fld}: {len(vals)} unique")
    for v in list(vals)[:10]:
        print(f"  - {v!r}")
