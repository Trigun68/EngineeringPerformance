# Excel Files Analysis Report - Database Design

## Overview

Found **14 Excel files** in .src directory. Three primary data domains identified:

| Domain | Primary File | Purpose |
|--------|-------------|---------|
| Work Norms Reference (Spravochnik) | Fizicheskie obemy.xlsx | Master catalog of construction work items with hierarchical codes, units, and resource rates |
| Labor Intensity (Trudoemkost) | Trudoemkost.xlsx | Project-level planned vs actual labor hours by discipline |
| Work Direction Norms (IIC) | Normy napravlenie rabot IIC.xlsx | 4-level hierarchical labor norms for concrete/steel works |

Additional files:
- KFO2.xlsx - Compact work classifier reference (288 rows, subset of full spravochnik)
- data - 2026-04-02T172124.400.xlsx - Identical to Trudoemkost.xlsx
- BER24-1 - kopiya.xlsx - Identical to Fizicheskie obemy.xlsx
- Spisok rabot 25-*.xlsx - Project-specific work lists (BoQ instances)

---

## FILE 1: Fizicheskie obemy.xlsx (Physical Volumes / Work Reference)

**Sheet:** Sheet (single sheet)
**Size:** 2,217 rows x 31 columns (A:AE)
**Merged cells:** 2,497 (heavy use of merging for hierarchical grouping)

### Structure

The file is a **master work catalog** (spravochnik) with 4 header rows and a 4-level hierarchical code system.

#### Header Rows

| Row | Role | Content |
|-----|------|---------|
| Row 1 | Title | Perechen rabot spravochnika (Work catalog list) |
| Row 2 | Section groups | Dannye po rabote (A), Opisatelnaya chasti i KFO (L), Rascenki resursov (U), Rascenki raboty (AB) |
| Row 3 | Column headers | Full column names (see below) |
| Row 4+ | Data rows | Hierarchical work items |

#### Column Definitions (with English translations)

| Col | Russian Name | English Name | Type | Notes |
|-----|-------------|--------------|------|-------|
| A | EP Kod i naimenovanie / Kod i naimenovanie resursa | EP Code and Name / Resource Code and Name | str | Hierarchical code (4 levels via columns A-D) |
| B | (merged with A for level 2) | Level 2 code+name | str | Format: 01-01 - Otdel 01. ... |
| C | (merged with A for level 3) | Level 3 code+name | str | Format: 01-01-001 - Razrabotka grunta... |
| D | (merged with A for level 4) | Level 4 code+name | str | Format: 01-01-001-01 - Razrabotka grunta... |
| E | EP Nepp | EP Sequence Number | float | Row ordinal within leaf items |
| F | EP Kod | EP Code | str | Unique work item code (e.g., 01-01-001-01) |
| G | EP Naimenovanie | EP Name/Description | str | Work item description |
| H | EP Sbornik | EP Collection/Book | str | Collection number (e.g., 01) |
| I | ER Ed. izm. | EP Unit of Measure | str | e.g., m3, m2, sht, t, m.p. |
| J | EP TZR | EP Transport and Storage Costs | float | Coefficient |
| K | EP Mashinochasy | EP Machine Hours | float | Machine hours per unit |
| L | Opisatelnaya chast | Description / Scope of Work | str | Multi-part description, numbered segments |
| M | KFO1 Kod | KFO1 Code | str | Work direction classifier code (e.g., KQ.02) |
| N | KFO1 Naimenovanie | KFO1 Name | str | Work direction name (e.g., Zemlyanye raboty) |
| O | KFO1 Ed. izm. | KFO1 Unit of Measure | str | Unit for KFO1 |
| P | KFO1 Koefficient | KFO1 Coefficient | float | Conversion factor (typically 1.0) |
| Q | KFO2 Kod | KFO2 Code | str | Sub-classifier code (e.g., KQ.02.110) |
| R | KFO2 Naimenovanie | KFO2 Name | str | Sub-direction name (e.g., Razrabotka grunta) |
| S | KFO2 Ed. izm. | KFO2 Unit of Measure | str | Unit for KFO2 |
| T | KFO2 Koefficient | KFO2 Coefficient | float | Conversion factor (typically 1.0) |
| U | Kod (shifr) resursa | Resource Code | str | Resource identifier |
| V | Naimenovanie resursa | Resource Name | str | Resource description |
| W | Ed. izm. resursa | Resource Unit | str | e.g., chel*ch, m3, kg |
| X | Raskhod na ed. obema rabot | Consumption per Unit of Work | float | Resource quantity per work unit |
| Y | Osnovnoy material dlya konstruktiva | Primary Material for Structure | str | Flag: 1 if primary material |
| Z | Stoimost ed. resursa | Unit Resource Cost | float | Cost per resource unit |
| AA | Obyazatelnyy dlya dobavleniya | Mandatory for Addition | str | Flag for required resources |
| AB | Kod (shifr) rascenki | Rate Code | str | Pricing/rate code |
| AC | Koef. smetnoy pribyli | Estimated Profit Coefficient | float | Profit margin factor |
| AD | Koef. nakladnykh raskhodov | Overhead Cost Coefficient | float | Overhead factor |
| AE | Koef. na subpodryad | Subcontractor Coefficient | float | Subcontractor factor |

### Hierarchical Structure

**4-level work breakdown structure** encoded in columns A-D:

Level 1 (Col A):  Sbornik (Collection)       - e.g., 01 - Sbornik 01. Zemlyanye raboty
Level 2 (Col B):  Otdel (Section)            - e.g., 01-01 - Otdel 01. Mekhanizirovannaya razrabotka gruntov
Level 3 (Col C):  Gruppa (Group)             - e.g., 01-01-001 - Razrabotka grunta ekskavatorami v otval
Level 4 (Col D):  Rascenka (Rate/Item)       - e.g., 01-01-001-01 - ... kovshom 0,5-1 m3, gruppa 1-2

Code pattern: XX-XX-XXX-XX (2-2-3-2 digits)

### Data Types Observed

| Type | Columns | Examples |
|------|---------|----------|
| str (codes) | A, F, M, Q, U, AB | 01-01-001-01, KQ.02, KQ.02.110 |
| str (names) | G, L, N, R, V | Zemlyanye raboty, Razrabotka grunta |
| str (units) | I, O, S, W | m3, m2, sht, t, m.p., chel*ch |
| float (quantities) | E, J, K, P, T, X, Z, AC, AD, AE | 1.0, 0.004, 0.03 |
| str (flags) | Y, AA | 1, empty |

### Special Formatting

- **Heavy merged cells** (2,497): Used for hierarchical grouping - columns A-C are merged for section headers
- **Multi-part descriptions** in column L: Numbered segments separated by periods
- **Row 1** is a title spanning all columns (A:AE merged)
- **Row 2** has section group headers spanning multiple columns

---

## FILE 2: Trudoemkost.xlsx (Labor Intensity)

**Sheet:** Sheet1 (single sheet)
**Size:** 776 rows x 11 columns (A:K)
**Merged cells:** None (flat structure)

### Structure

A **flat summary table** of planned vs actual labor hours by project and discipline.

#### Column Definitions

| Col | Russian Name | English Name | Type | Notes |
|-----|-------------|--------------|------|-------|
| A | Nomer proekta | Project Number | str | e.g., 25-310, 25-970 |
| B | Naimenovanie proekta | Project Name | str | e.g., Programma uvelicheniya proizvodstva... |
| C | Marka | Brand/Mark | str | Appears empty in most rows |
| D | Naimenovanie proektnogo napravleniya | Project Discipline Name | str | e.g., Avtomatizatsiya, Arkhitektura |
| E | Summa Planovaya trudoemkost, cch | Total Planned Labor Hours | float | Hours (cch = cheloveko-chasy) |
| F | Summa Fakticheski spisano, cch | Total Actual Written-off Hours | float | Hours |

### Disciplines Observed (Column D)

- Avtomatizatsiya (kompleksnaya, v t.ch. otdelnykh sistem) - Automation
- Arkhitektura - Architecture
- Ventilyatsiya i konditsionirovanie - Ventilation and AC
- Zhelezobeton. Karkas - Reinforced Concrete Frame
- Metallokonstruktsii - Steel Structures
- Mekhanicheskaya chast (Montazhnaya i Truboprovody) - Mechanical (Installation and Pipelines)
- Organizatsiya stroitelstva - Construction Organization
- Osveshchenie - Lighting
- Otoplenie i teplosnabzhenie - Heating and Heat Supply
- Promyshlennyy provodki (gazosnabzhenie) - Industrial Wiring (Gas Supply)
- Promyshlennyy transport i Gruzopodemnye mekhanizmy - Industrial Transport and Lifting
- Raspredelitelnye i pitayushchie elektricheskie seti - Distribution and Power Networks
- Seti svyazi i signalizatsii - Communication and Alarm Networks
- Silovoe elektrooborudovanie - Power Electrical Equipment

---

## FILE 3: Normy napravlenie rabot IIC.xlsx (Work Direction Norms IIC)

**Sheet:** spr2 (single sheet)
**Size:** 75 rows x 6 columns (A:F)
**Merged cells:** None

### Structure

A **compact 4-level hierarchy** of labor norms for concrete and steel construction work.

#### Column Definitions

| Col | Russian Name | English Name | Type | Notes |
|-----|-------------|--------------|------|-------|
| A | Ne | Number/Code | str | Hierarchical: 1., 1.1., 1.1.1., 1.1.2. |
| B | 2 uroven | Level 2 | str | Category (e.g., Obshchiy beton, Stalnye konstruktsii) |
| C | 3 uroven | Level 3 | str | Sub-category (e.g., Obshchie plity, Obshchie fundamenty) |
| D | 4 uroven | Level 4 | str | Specific item (e.g., Plity na grunte, Svai) |
| E | Edinitsa izm. | Unit of Measure | str | e.g., chas/yard3, chas/tonna, chas/sht |
| F | Opisanie | Description | str | Detailed scope description |

### Hierarchical Structure

Level 2 (Col B):  Category           - e.g., Obshchiy beton (General Concrete)
Level 3 (Col C):  Sub-category       - e.g., Obshchie plity (General Slabs)
Level 4 (Col D):  Specific item      - e.g., Plity na grunte i opertye plity (Slabs on grade)

### Categories Observed

| Level 2 | Level 3 Examples | Level 4 Examples | Unit |
|---------|-----------------|------------------|------|
| Obshchiy beton (General Concrete) | Obshchie plity, Obshchie fundamenty, Betonnye konstruktsii | Plity na grunte, Fundamenty (<5 yard3), Svai | chas/yard3, chas/sht |
| Stalnye konstruktsii (Steel Structures) | Vspomogatelnye stalnye konstruktsii, Kombinirovannye stalnye konstruktsii/estakady | Stalnye konstruktsii, Estakady i kommunikatsionnye mosty | chas/tonna |

---

## FILE 4: KFO2.xlsx (Work Classifier Reference)

**Sheet:** KFO2 (single sheet)
**Size:** 288 rows x 30 columns (A:AD)
**Merged cells:** 291

### Structure

A **compact work classifier** - essentially a subset/variant of the full Fizicheskie obemy.xlsx spravochnik. Same column structure but fewer rows (288 vs 2,217).

Uses the same 4-level hierarchy and KFO1/KFO2 classifier system.

---

## FILE 5: Spisok rabot 25-310.xlsx (Work List for Project 25-310)

**Sheet:** Spisok rabot (single sheet)
**Size:** 11,437 rows x 23 columns (A:W)
**Merged cells:** 7 (minimal)

### Structure

A **project-specific work list** (BoQ instance) linking work items to specific facilities and quantities.

#### Key Columns

| Col | Russian Name | English Name | Type | Notes |
|-----|-------------|--------------|------|-------|
| A | Ne pozitsii v osnovnom spiske | Position Number | float | e.g., 1.0 |
| B | Kod proekta | Project Code | str | e.g., 25-310 |
| C | FBS (Code and Title of Facility) | Facility Breakdown Structure | str | e.g., 4A0400 - Zdanie sklada slyabov |
| D | Tip privyazki | Binding Type | str | e.g., Rabota k edinitsye oborudovaniya |
| E | Komplekt dokumentov | Document Set | str | e.g., 4A0000-OP-PZ3 |
| F | ID po katalogu | Catalog ID | str | e.g., 13-06-001-01 |
| G | TAG | Tag | str | Equipment tag |
| H | ID raboty polnyy | Full Work ID | str | e.g., 13-06-001-01 |
| I | Naimenovanie raboty | Work Name | str | e.g., Ustanovka kamer pritochnykh... |
| J | Kommentariy k rabote | Work Comment | str | e.g., OV, ES. YaKNO |
| K | Opisatelnaya chast | Description | str | Scope description |
| L | KFO1 Kod | KFO1 Code | str | e.g., KQ.16 |
| M | KFO1 Naimenovanie | KFO1 Name | str | e.g., Montazh setey ventilyatsii |
| N | KFO2 Kod | KFO2 Code | str | e.g., KQ.16.180 |
| O | KFO2 Naimenovanie | KFO2 Name | str | e.g., Montazh oborudovaniya konditsionirovaniya |
| P | Ed. izm. | Unit of Measure | str | e.g., sht. |
| Q | Kol-vo po BoQ | BoQ Quantity | float | e.g., 7.0 |
| R | Bazis otsenki | Estimation Basis | str | e.g., I - Istoricheskiye dannyye... |
| S | Trudozatrati, cch/1ed. | Labor Hours per Unit | float | e.g., 45.896 |
| T | Mashinochasi, cch/1ed. | Machine Hours per Unit | float | e.g., 0.23 |
| U | Itogovaya trudoemkost, cch | Total Labor Hours | float | e.g., 321.272 |
| V | Itogovyye mashinochasi, cch | Total Machine Hours | float | e.g., 1.61 |
| W | Status pozitsii | Position Status | str | e.g., V rabote |

---

## Database Design Recommendations

### Core Tables

#### 1. project
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| code | VARCHAR(20) UNIQUE | e.g., 25-310, 25-970 |
| name | VARCHAR(500) | |
| brand | VARCHAR(100) | Optional |

#### 2. discipline
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| name_ru | VARCHAR(500) | Russian name |
| name_en | VARCHAR(500) | English translation |

#### 3. project_discipline_labor
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| project_id | FK to project | |
| discipline_id | FK to discipline | |
| planned_hours | DECIMAL(12,2) | Planovaya trudoemkost |
| actual_hours | DECIMAL(12,2) | Fakticheski spisano |

#### 4. work_collection (Sbornik - Level 1)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| code | VARCHAR(10) UNIQUE | e.g., 01 |
| name_ru | VARCHAR(500) | e.g., Sbornik 01. Zemlyanye raboty |

#### 5. work_section (Otdel - Level 2)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| collection_id | FK to work_collection | |
| code | VARCHAR(20) UNIQUE | e.g., 01-01 |
| name_ru | VARCHAR(500) | |

#### 6. work_group (Gruppa - Level 3)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| section_id | FK to work_section | |
| code | VARCHAR(30) UNIQUE | e.g., 01-01-001 |
| name_ru | VARCHAR(500) | |

#### 7. work_item (Rascenka - Level 4, leaf)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| group_id | FK to work_group | |
| code | VARCHAR(50) UNIQUE | e.g., 01-01-001-01 |
| name_ru | VARCHAR(1000) | |
| unit_of_measure | VARCHAR(20) | e.g., m3, m2, sht |
| sequence_number | INT | Nepp |
| transport_storage_coef | DECIMAL(10,4) | TZR |
| machine_hours_per_unit | DECIMAL(10,4) | Mashinochasy |
| description | TEXT | Opisatelnaya chast |

#### 8. kfo_classifier (KFO1 - Work Direction Classifier)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| code | VARCHAR(20) UNIQUE | e.g., KQ.02 |
| name_ru | VARCHAR(500) | e.g., Zemlyanye raboty |
| unit_of_measure | VARCHAR(20) | |

#### 9. kfo_subclassifier (KFO2 - Sub-classifier)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| kfo_id | FK to kfo_classifier | |
| code | VARCHAR(30) UNIQUE | e.g., KQ.02.110 |
| name_ru | VARCHAR(500) | e.g., Razrabotka grunta |
| unit_of_measure | VARCHAR(20) | |

#### 10. work_item_kfo_mapping
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| work_item_id | FK to work_item | |
| kfo_id | FK to kfo_classifier | |
| kfo_coef | DECIMAL(10,4) | KFO1 Koefficient |
| kfo_sub_id | FK to kfo_subclassifier | |
| kfo_sub_coef | DECIMAL(10,4) | KFO2 Koefficient |

#### 11. resource
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| code | VARCHAR(50) | Kod resursa |
| name_ru | VARCHAR(500) | Naimenovanie resursa |
| unit_of_measure | VARCHAR(20) | Ed. izm. resursa |
| unit_cost | DECIMAL(12,4) | Stoimost ed. resursa |

#### 12. work_item_resource
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| work_item_id | FK to work_item | |
| resource_id | FK to resource | |
| consumption_per_unit | DECIMAL(12,4) | Raskhod na ed. obema rabot |
| is_primary_material | BOOLEAN | Osnovnoy material |
| is_mandatory | BOOLEAN | Obyazatelnyy dlya dobavleniya |

#### 13. work_item_rate
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| work_item_id | FK to work_item | |
| rate_code | VARCHAR(50) | Kod rascenki |
| profit_coef | DECIMAL(10,4) | Koef. smetnoy pribyli |
| overhead_coef | DECIMAL(10,4) | Koef. nakladnykh raskhodov |
| subcontractor_coef | DECIMAL(10,4) | Koef. na subpodryad |

#### 14. facility (FBS - Facility Breakdown Structure)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| project_id | FK to project | |
| code | VARCHAR(50) | e.g., 4A0400 |
| name_ru | VARCHAR(500) | e.g., Zdanie sklada slyabov |

#### 15. project_work_item (BoQ instance - links work items to projects/facilities)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| project_id | FK to project | |
| facility_id | FK to facility | |
| work_item_id | FK to work_item | |
| position_number | DECIMAL(10,1) | Ne pozitsii |
| binding_type | VARCHAR(200) | Tip privyazki |
| document_set | VARCHAR(100) | Komplekt dokumentov |
| catalog_id | VARCHAR(100) | ID po katalogu |
| tag | VARCHAR(100) | TAG |
| work_name_override | VARCHAR(1000) | Custom name if different |
| comment | VARCHAR(500) | Kommentariy |
| quantity | DECIMAL(12,4) | Kol-vo po BoQ |
| estimation_basis | VARCHAR(500) | Bazis otsenki |
| labor_hours_per_unit | DECIMAL(12,4) | Trudozatrati |
| machine_hours_per_unit | DECIMAL(12,4) | Mashinochasi |
| total_labor_hours | DECIMAL(12,4) | Itogovaya trudoemkost |
| total_machine_hours | DECIMAL(12,4) | Itogovyye mashinochasi |
| status | VARCHAR(50) | Status pozitsii |

#### 16. iic_work_norm (IIC norms - separate hierarchy)
| Column | Type | Notes |
|--------|------|-------|
| id | UUID PK | |
| parent_id | FK to iic_work_norm (self) | For hierarchy |
| level | INT | 2, 3, or 4 |
| code | VARCHAR(20) | e.g., 1., 1.1., 1.1.1. |
| name_ru | VARCHAR(500) | |
| unit_of_measure | VARCHAR(30) | e.g., chas/yard3 |
| description | TEXT | |

### Key Relationships

project --< project_discipline_labor >-- discipline

work_collection --< work_section --< work_group --< work_item
                                                          |
                                                          |--< work_item_kfo_mapping >-- kfo_classifier
                                                          |                                    |
                                                          |                                    --< kfo_subclassifier
                                                          |
                                                          |--< work_item_resource >-- resource
                                                          |
                                                          --< work_item_rate

project --< facility
project --< project_work_item >-- work_item
facility --< project_work_item

iic_work_norm (self-referencing hierarchy)

### Design Notes

1. **Hierarchical work items**: The 4-level hierarchy (Collection to Section to Group to Item) is normalized into separate tables rather than using a self-referencing table, because each level has distinct semantics and code patterns.

2. **KFO classifiers**: Two-level classifier (KFO1 to KFO2) is normalized. KFO2 always belongs to a KFO1 parent.

3. **Merged cells in Excel**: The 2,497 merged cells in Fizicheskie obemy.xlsx represent hierarchical grouping. When importing, use the code pattern in columns A-D to determine parent-child relationships.

4. **Units of measure**: Multiple unit systems exist (metric m3/m2/sht/t in spravochnik, imperial yard3/tonna in IIC). Consider a unit_of_measure reference table for normalization.

5. **Project work lists** (Spisok rabot 25-*.xlsx): Each project has its own file. These are BoQ instances that reference work items from the spravochnik and add project-specific quantities, costs, and status.
