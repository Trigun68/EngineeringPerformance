"""Generate 004_LaborIntensity.sql seed from trud_data.json.

Hierarchy:
  L1 nodes  — Код ИСР  (40)
  L2 nodes  — Код направления  (40)
  L3 nodes  — Тип документа    (521)

Texas → IIC mapping: '3.5.2.' -> 'IIC-3-5-2' (lookup by IicWorkDirections.Code).
"""
import json, io, sys, re
from collections import defaultdict
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

with open(r"C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\_dev\trud_data.json", encoding="utf-8") as f:
    data = json.load(f)


def texas_to_iic(t):
    if not t:
        return None
    parts = [p for p in t.strip(".").split(".") if p]
    return "IIC-" + "-".join(parts) if parts else None


def sql_str(s):
    if s is None:
        return "NULL"
    s = str(s).replace("'", "''")
    return "N'" + s + "'"


def sql_num(s):
    if s is None or s == "":
        return "NULL"
    try:
        return str(float(s))
    except (TypeError, ValueError):
        return "NULL"


# Group by WBS code (preserving first-seen order)
wbs_order = []
wbs_to_dir = {}
for d in data:
    wbs = d["Код ИСР"]
    if wbs not in wbs_to_dir:
        wbs_order.append(wbs)
        wbs_to_dir[wbs] = (d["Код направления"], d["Направление"])

children_by_wbs = defaultdict(list)
for d in data:
    children_by_wbs[d["Код ИСР"]].append(d)

out = []
out.append("-- ============================================================================")
out.append("-- Seed Data (004): Labor Intensity — справочник трудоёмкости (вкладка Труд из")
out.append("--   .src/Рабочие материалы v.3.xlsx). Дерево L1=Код ИСР, L2=Код направления,")
out.append("--   L3=Тип документа. Поле TexasCode связывает лист со справочником")
out.append("--   IicWorkDirections (Техас '3.5.2.' -> IIC-3-5-2).")
out.append("-- ============================================================================")
out.append("SET NOCOUNT ON;")
out.append("BEGIN TRANSACTION;")
out.append("")
out.append("IF NOT EXISTS (SELECT 1 FROM [LaborIntensities])")
out.append("BEGIN")
out.append("    DECLARE @ParentId INT;")
out.append("")

for wbs_idx, wbs in enumerate(wbs_order):
    dir_code, dir_name = wbs_to_dir[wbs]
    out.append(f"    -- L1: {wbs}")
    out.append(
        "    INSERT INTO [LaborIntensities] ([Code], [NodeLevel], [Name], [ParentId], [SortOrder], [IsArchived], [CreatedAt]) "
        f"VALUES ({sql_str(wbs)}, 1, {sql_str(wbs)}, NULL, {wbs_idx}, 0, SYSUTCDATETIME());"
    )
    out.append("    SET @ParentId = SCOPE_IDENTITY();")
    out.append(
        "    INSERT INTO [LaborIntensities] ([Code], [NodeLevel], [Name], [ParentId], [SortOrder], [IsArchived], [CreatedAt]) "
        f"VALUES ({sql_str(dir_code)}, 2, {sql_str(dir_name)}, @ParentId, 0, 0, SYSUTCDATETIME());"
    )
    out.append("    DECLARE @DirId INT = SCOPE_IDENTITY();")
    for sort_idx, row in enumerate(children_by_wbs[wbs]):
        doc_id = row["Идентификатор типа документа"]
        doc_name = row["Тип документа"]
        ppr, opr, pd, rd = row["Норматив ППР"], row["Норматив ОПР"], row["Норматив ПД"], row["Норматив РД"]
        texas = row["Техас"]
        iic_code = texas_to_iic(texas)
        if iic_code:
            iic_lookup = f"(SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = {sql_str(iic_code)})"
        else:
            iic_lookup = "NULL"
        out.append(
            "    INSERT INTO [LaborIntensities] ([Code], [NodeLevel], [Name], [DocumentTypeName], "
            "[NormPpr], [NormOpr], [NormPd], [NormRd], [TexasCode], [IicWorkDirectionId], "
            "[ParentId], [SortOrder], [IsArchived], [CreatedAt]) VALUES ("
            f"{sql_str(doc_id)}, 3, {sql_str(doc_name)}, {sql_str(doc_name)}, "
            f"{sql_num(ppr)}, {sql_num(opr)}, {sql_num(pd)}, {sql_num(rd)}, "
            f"{sql_str(texas)}, {iic_lookup}, "
            f"@DirId, {sort_idx}, 0, SYSUTCDATETIME());"
        )
    out.append("")

out.append("END;")
out.append("COMMIT;")
out.append("GO")
out.append("")

# T-SQL doesn't allow re-DECLARE; rewrite to use single declaration.
text = "\n".join(out)
# Move DECLAREs to top, remove duplicates inside loop
text = text.replace("    DECLARE @DirId INT = SCOPE_IDENTITY();", "    SET @DirId = SCOPE_IDENTITY();")
text = text.replace(
    "    DECLARE @ParentId INT;\n",
    "    DECLARE @ParentId INT;\n    DECLARE @DirId INT;\n",
    1,
)

with open(r"C:\Users\elise\YandexDisk\!Project\EngineeringPerformance\migrations\data\004_LaborIntensity.sql", "w", encoding="utf-8") as f:
    f.write(text)
print(f"Wrote 004_LaborIntensity.sql, {len(text)} chars")
