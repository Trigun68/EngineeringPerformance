import json, io, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

with open(r"C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\_dev\trud_data.json", encoding="utf-8") as f:
    data = json.load(f)

# group by WbsCode -> set of (DirectionCode, Direction name)
from collections import defaultdict
m = defaultdict(set)
for d in data:
    m[d["Код ИСР"]].add((d["Код направления"], d["Направление"]))
multi = {k:v for k,v in m.items() if len(v) > 1}
print(f"Wbs codes with >1 direction: {len(multi)}")
for k,v in list(multi.items())[:5]:
    print(k, v)
print(f"Total wbs codes: {len(m)}, total directions across: {sum(len(v) for v in m.values())}")
