import json, io, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

with open(r"C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\_dev\trud_data.json", encoding="utf-8") as f:
    data = json.load(f)

texas_vals = sorted({d["Техас"] for d in data if d["Техас"]})
print("All Texas values:")
for v in texas_vals:
    parts = [p for p in v.strip(".").split(".") if p]
    iic = "IIC-" + "-".join(parts) if parts else None
    print(f"  {v!r}  ->  {iic}")
