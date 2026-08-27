-- =============================================================================
-- 021_RelinkProjectEquipmentToActiveCatalog.sql
-- Перепривязка ProjectEquipments с архивных позиций на активные двойники
-- справочника с тем же наименованием (приоритет: совпадение UnitId).
--
-- Контекст: после обновления номенклатуры оборудования строки проектов остались
-- на archived-двойниках с IsAssessment=0, тогда как активная позиция имеет
-- IsAssessment=1. Прогноз/факт учитывают только оценку → заниженный объём
-- (пример: проект 25-310, план 30.06.26 — было ~310 вместо ~622 по каталогу).
--
-- Идемпотентно. Бэкап: bak_ProjectEquipments_relink_20260722.
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.bak_ProjectEquipments_relink_20260722', N'U') IS NULL
BEGIN
    SELECT pe.*
    INTO dbo.bak_ProjectEquipments_relink_20260722
    FROM dbo.ProjectEquipments pe;
END
GO

BEGIN TRANSACTION;

;WITH ActiveCandidates AS (
    SELECT
        a.Id AS ActiveId,
        a.Name,
        a.UnitId,
        a.IsAssessment,
        COUNT(*) OVER (
            PARTITION BY a.Name, ISNULL(a.UnitId, -1)
        ) AS CntSameUnit,
        COUNT(*) OVER (PARTITION BY a.Name) AS CntSameName
    FROM dbo.Equipments a
    WHERE a.IsArchived = 0
),
Resolved AS (
    -- 1) Однозначный активный по (Name, UnitId)
    SELECT pe.Id AS ProjectEquipmentId, ac.ActiveId
    FROM dbo.ProjectEquipments pe
    INNER JOIN dbo.Equipments oldEq ON oldEq.Id = pe.EquipmentId
    INNER JOIN ActiveCandidates ac
        ON ac.Name = oldEq.Name
       AND ISNULL(ac.UnitId, -1) = ISNULL(oldEq.UnitId, -1)
       AND ac.CntSameUnit = 1
    WHERE oldEq.IsArchived = 1
      AND pe.EquipmentId <> ac.ActiveId

    UNION ALL

    -- 2) Иначе — однозначный активный только по Name (UnitId разъехался)
    SELECT pe.Id AS ProjectEquipmentId, ac.ActiveId
    FROM dbo.ProjectEquipments pe
    INNER JOIN dbo.Equipments oldEq ON oldEq.Id = pe.EquipmentId
    INNER JOIN ActiveCandidates ac
        ON ac.Name = oldEq.Name
       AND ac.CntSameName = 1
    WHERE oldEq.IsArchived = 1
      AND pe.EquipmentId <> ac.ActiveId
      AND NOT EXISTS (
          SELECT 1
          FROM ActiveCandidates x
          WHERE x.Name = oldEq.Name
            AND ISNULL(x.UnitId, -1) = ISNULL(oldEq.UnitId, -1)
            AND x.CntSameUnit = 1
      )
)
UPDATE pe
SET pe.EquipmentId = r.ActiveId
FROM dbo.ProjectEquipments pe
INNER JOIN Resolved r ON r.ProjectEquipmentId = pe.Id;

DECLARE @updated INT = @@ROWCOUNT;
PRINT CONCAT('ProjectEquipments relinked to active catalog: ', @updated);

COMMIT TRANSACTION;
GO
