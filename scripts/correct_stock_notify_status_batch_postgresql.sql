-- 分批到货/出库：校正到货通知、出库通知业务状态（与代码 Recalculate 口径一致，可单独执行）
--
-- 背景：
--   · 入库：同采购明细多条到货通知时，扩展重算曾误用「整行质检单」判断已入库，导致未入库通知 Status=100。
--   · 出库：装箱同步曾误将「同销售行全部出库通知」标为已出库(100)。
--
-- 规则（与 PurchaseOrderItemExtendSyncService / PackingService 修复后一致）：
--   到货通知：100=本通知有关联已过账采购入库；30=有质检；20=ReceiveQty>0；否则 10。
--   出库通知：100=本通知 SourceId 关联已出库/已完成销售出库单；5=报关中；20=有未完成装箱单明细；否则 10。
--
-- 执行后建议在系统内对涉及采购/销售明细执行一次「扩展刷新」，或等待下次业务触发 RecalculateAsync。

BEGIN;

-- ---------------------------------------------------------------------------
-- 1) 到货通知 stockin_notify
-- ---------------------------------------------------------------------------
WITH arrival_eval AS (
    SELECT
        n."UserId" AS id,
        n."NoticeCode" AS notice_code,
        n."Status" AS old_status,
        CASE
            WHEN EXISTS (
                SELECT 1
                FROM stock_in si
                WHERE si.is_deleted = FALSE
                  AND si."Status" = 2
                  AND si."StockInType" = 10
                  AND (
                      si."SourceId" = n."UserId"
                      OR EXISTS (
                          SELECT 1
                          FROM qcinfo q
                          WHERE q.is_deleted = FALSE
                            AND q."StockInNotifyId" = n."UserId"
                            AND (
                                (si."QCID" IS NOT NULL AND si."QCID" = q."UserId")
                                OR (q."StockInId" IS NOT NULL AND q."StockInId" = si."StockInId")
                            )
                      )
                  )
            ) THEN 100
            WHEN EXISTS (
                SELECT 1
                FROM qcinfo q
                WHERE q.is_deleted = FALSE
                  AND q."StockInNotifyId" = n."UserId"
            ) THEN 30
            WHEN n."ReceiveQty" > 0 THEN 20
            ELSE 10
        END AS new_status
    FROM stockin_notify n
    WHERE n.is_deleted = FALSE
),
arrival_changed AS (
    SELECT *
    FROM arrival_eval
    WHERE old_status IS DISTINCT FROM new_status
)
UPDATE stockin_notify n
SET
    "Status" = c.new_status,
    "ModifyTime" = NOW() AT TIME ZONE 'utc'
FROM arrival_changed c
WHERE n."UserId" = c.id;

-- 预览（执行前可先单独运行下面 SELECT 查看将变更的行）
-- SELECT notice_code, old_status, new_status FROM arrival_changed ORDER BY notice_code;

-- ---------------------------------------------------------------------------
-- 2) 出库通知 stockout_notify
-- ---------------------------------------------------------------------------
WITH stockout_eval AS (
    SELECT
        r."ID" AS id,
        r."Code" AS request_code,
        r."Status" AS old_status,
        CASE
            WHEN r."Status" = -1 THEN -1
            WHEN EXISTS (
                SELECT 1
                FROM stock_out so
                WHERE so.is_deleted = FALSE
                  AND so."Status" IN (2, 4)
                  AND so."StockOutType" = 10
                  AND so."SourceId" = r."ID"
            ) THEN 100
            WHEN r."CustomsStatus" IN (20, 30) THEN 5
            WHEN EXISTS (
                SELECT 1
                FROM packing_item pi
                INNER JOIN packing p
                    ON p."Id" = pi."PackingId"
                   AND p.is_deleted = FALSE
                WHERE pi.is_deleted = FALSE
                  AND pi.stockout_notify_id = r."ID"
                  AND p."Status" < 100
            ) THEN 20
            ELSE 10
        END AS new_status
    FROM stockout_notify r
    WHERE r.is_deleted = FALSE
),
stockout_changed AS (
    SELECT *
    FROM stockout_eval
    WHERE old_status IS DISTINCT FROM new_status
)
UPDATE stockout_notify r
SET
    "Status" = c.new_status,
    "ModifyTime" = NOW() AT TIME ZONE 'utc'
FROM stockout_changed c
WHERE r."ID" = c.id;

COMMIT;

-- ---------------------------------------------------------------------------
-- 3) 校正结果摘要（执行后查看）
-- ---------------------------------------------------------------------------
SELECT 'stockin_notify' AS entity,
       COUNT(*) AS rows_still_status_100
FROM stockin_notify n
WHERE n.is_deleted = FALSE
  AND n."Status" = 100
  AND NOT EXISTS (
      SELECT 1
      FROM stock_in si
      WHERE si.is_deleted = FALSE
        AND si."Status" = 2
        AND si."StockInType" = 10
        AND (
            si."SourceId" = n."UserId"
            OR EXISTS (
                SELECT 1
                FROM qcinfo q
                WHERE q.is_deleted = FALSE
                  AND q."StockInNotifyId" = n."UserId"
                  AND (
                      (si."QCID" IS NOT NULL AND si."QCID" = q."UserId")
                      OR (q."StockInId" IS NOT NULL AND q."StockInId" = si."StockInId")
                  )
            )
        )
  );

SELECT 'stockout_notify' AS entity,
       COUNT(*) AS rows_still_status_100_without_stock_out
FROM stockout_notify r
WHERE r.is_deleted = FALSE
  AND r."Status" = 100
  AND NOT EXISTS (
      SELECT 1
      FROM stock_out so
      WHERE so.is_deleted = FALSE
        AND so."Status" IN (2, 4)
        AND so."StockOutType" = 10
        AND so."SourceId" = r."ID"
  );
