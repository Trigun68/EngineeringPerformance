import openpyxl, json, sys

path = r"C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\.src\Рабочие материалы v.3.xlsx"
wb = openpyxl.load_workbook(path, data_only=True)
print("Sheets:", wb.sheetnames)

# find sheet with "Труд" in name
sheet = next((s for s in wb.sheetnames if "Труд" in s), None)
if not sheet:
    print("Sheet not found"); sys.exit(1)

ws = wb[sheet]
print(f"Sheet: {sheet}, dims: {ws.dimensions}")

# print first 3 rows to see headers
for i, row in enumerate(ws.iter_rows(values_only=True)):
    if i >= 3: break
    print(f"Row {i+1}:", row)
