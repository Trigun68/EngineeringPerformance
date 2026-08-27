-- ============================================================================
-- Unarchive equipment linked to project data (auto-created by imports)
-- that nomenclature refresh (014/019) archived because CatalogKeyHash
-- is not in official «Спр_Оборудование».
--
-- Idempotent. Safe for docker-db-init after 014.
-- Also relinks PE to an active twin when one exists (Name+UnitId).
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRANSACTION;

-- 1) Перепривязка на активный двойник (Name + UnitId), если он один.
;WITH ActiveCandidates AS (
    SELECT
        a.Id AS ActiveId,
        a.Name,
        a.UnitId,
        COUNT(*) OVER (
            PARTITION BY a.Name, ISNULL(a.UnitId, -1)
        ) AS CntSameUnit
    FROM dbo.Equipments a
    WHERE a.IsArchived = 0
),
Resolved AS (
    SELECT pe.Id AS ProjectEquipmentId, ac.ActiveId
    FROM dbo.ProjectEquipments pe
    INNER JOIN dbo.Equipments oldEq ON oldEq.Id = pe.EquipmentId
    INNER JOIN ActiveCandidates ac
        ON ac.Name = oldEq.Name
       AND ISNULL(ac.UnitId, -1) = ISNULL(oldEq.UnitId, -1)
       AND ac.CntSameUnit = 1
    WHERE oldEq.IsArchived = 1
      AND pe.EquipmentId <> ac.ActiveId
)
UPDATE pe
SET pe.EquipmentId = r.ActiveId
FROM dbo.ProjectEquipments pe
INNER JOIN Resolved r ON r.ProjectEquipmentId = pe.Id;

DECLARE @Relinked int = @@ROWCOUNT;
PRINT CONCAT(N'Relinked ProjectEquipments to active twin: ', @Relinked);

-- 2) Разархивировать всё ещё привязанное к проектам (нет активного двойника).
UPDATE e
SET e.IsArchived = 0,
    e.ModifiedAt = SYSUTCDATETIME()
FROM dbo.Equipments e
WHERE e.IsArchived = 1
  AND EXISTS (
      SELECT 1
      FROM dbo.ProjectEquipments pe
      WHERE pe.EquipmentId = e.Id
  );

DECLARE @Unarchived int = @@ROWCOUNT;
PRINT CONCAT(N'Unarchived project-linked equipment: ', @Unarchived);

COMMIT TRANSACTION;
