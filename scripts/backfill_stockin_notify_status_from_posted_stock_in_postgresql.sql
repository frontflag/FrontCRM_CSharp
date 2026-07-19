-- 到货通知状态批量校正：与 PurchaseOrderItemExtendSyncService.RecalculateArrivalNoticeStatusesForPoLineAsync 口径一致
--
-- 推荐：在 Debug → 数据 页点击「刷新到货通知状态」（POST /api/v1/debug/refresh-arrival-notice-statuses），与下列 SQL 同源。
--
-- 典型问题：已有已过账采购入库单（stock_in.Status=2），但 stockin_notify.Status 仍停在 30（已质检）。
--
-- 【预览无结果 ≠ 一定没问题】
--   若关联字段对不上（SourceId/QCID 未写、有空格、大小写不一致），算出的 new_status 仍为 30，
--   与 old_status 相同，预览 WHERE old_status <> new_status 会过滤掉 → 看起来「没有要改的」。
--   请先跑「0) 诊断」，看 status=30 且已有过账入库的行是否存在。
--
-- 规则：
--   100 = 本通知有关联已过账采购入库（SourceId 或 qcinfo↔stock_in 链路）
--   30  = 有质检单、尚无上述入库
--   20  = ReceiveQty>0、无质检
--   10  = 其余

-- ---------------------------------------------------------------------------
-- 0) 诊断（建议先跑）
-- ---------------------------------------------------------------------------

-- 0a) 仍为「已质检(30)」的到货通知数量
SELECT COUNT(*) AS status_30_count
FROM public.stockin_notify n
WHERE COALESCE(n.is_deleted, false) = false
  AND n."Status" = 30;

-- 0b) status=30 且能关联到「已过账(status=2)」入库单（宽松：不限 StockInType，便于发现关联断裂）
SELECT
    n."NoticeCode" AS notice_code,
    n."Status" AS notice_status,
    si."StockInCode" AS stock_in_code,
    si."Status" AS stock_in_status,
    si."StockInType" AS stock_in_type,
    si."SourceId" AS stock_in_source_id,
    n."UserId" AS notice_id,
    si."QCID" AS stock_in_qc_id,
    q."UserId" AS qc_id,
    q."StockInId" AS qc_stock_in_id
FROM public.stockin_notify n
LEFT JOIN public.qcinfo q
    ON COALESCE(q.is_deleted, false) = false
   AND lower(btrim(q."StockInNotifyId")) = lower(btrim(n."UserId"))
LEFT JOIN public.stock_in si
    ON COALESCE(si.is_deleted, false) = false
   AND si."Status" = 2
   AND (
       lower(btrim(si."SourceId")) = lower(btrim(n."UserId"))
       OR (q."UserId" IS NOT NULL AND lower(btrim(si."QCID")) = lower(btrim(q."UserId")))
       OR (q."StockInId" IS NOT NULL AND lower(btrim(q."StockInId")) = lower(btrim(si."StockInId")))
   )
WHERE COALESCE(n.is_deleted, false) = false
  AND n."Status" = 30
  AND si."StockInId" IS NOT NULL
ORDER BY n."NoticeCode";

-- 0c) 按规则（含历史 stock_in.StockInType=1 等）应升为 100、但当前仍为 30 的行数
SELECT COUNT(*) AS should_be_100_still_30
FROM public.stockin_notify n
WHERE COALESCE(n.is_deleted, false) = false
  AND n."Status" = 30
  AND EXISTS (
      SELECT 1
      FROM public.stock_in si
      WHERE COALESCE(si.is_deleted, false) = false
        AND si."Status" = 2
        AND si."StockInType" <> 3
        AND CASE
            WHEN si."StockInType" IN (10, 20, 30, 40) THEN si."StockInType"
            ELSE 10
        END = CASE
            WHEN n."StockInType" IN (10, 20, 30, 40) THEN n."StockInType"
            ELSE 10
        END
        AND (
            lower(btrim(si."SourceId")) = lower(btrim(n."UserId"))
            OR EXISTS (
                SELECT 1
                FROM public.qcinfo q
                WHERE COALESCE(q.is_deleted, false) = false
                  AND lower(btrim(q."StockInNotifyId")) = lower(btrim(n."UserId"))
                  AND (
                      (si."QCID" IS NOT NULL AND lower(btrim(si."QCID")) = lower(btrim(q."UserId")))
                      OR (q."StockInId" IS NOT NULL AND lower(btrim(q."StockInId")) = lower(btrim(si."StockInId")))
                  )
            )
        )
  );

-- ---------------------------------------------------------------------------
-- 1) 预览：将发生变更的行（重点 old_status=30 AND new_status=100）
-- ---------------------------------------------------------------------------
WITH arrival_eval AS (
    SELECT
        n."UserId" AS id,
        n."NoticeCode" AS notice_code,
        n."Status" AS old_status,
        CASE
            WHEN EXISTS (
                SELECT 1
                FROM public.stock_in si
                WHERE COALESCE(si.is_deleted, false) = false
                  AND si."Status" = 2
                  AND si."StockInType" <> 3
                  AND CASE
                      WHEN si."StockInType" IN (10, 20, 30, 40) THEN si."StockInType"
                      ELSE 10
                  END = CASE
                      WHEN n."StockInType" IN (10, 20, 30, 40) THEN n."StockInType"
                      ELSE 10
                  END
                  AND (
                      lower(btrim(si."SourceId")) = lower(btrim(n."UserId"))
                      OR EXISTS (
                          SELECT 1
                          FROM public.qcinfo q
                          WHERE COALESCE(q.is_deleted, false) = false
                            AND lower(btrim(q."StockInNotifyId")) = lower(btrim(n."UserId"))
                            AND (
                                (si."QCID" IS NOT NULL AND lower(btrim(si."QCID")) = lower(btrim(q."UserId")))
                                OR (q."StockInId" IS NOT NULL AND lower(btrim(q."StockInId")) = lower(btrim(si."StockInId")))
                            )
                      )
                  )
            ) THEN 100
            WHEN EXISTS (
                SELECT 1
                FROM public.qcinfo q
                WHERE COALESCE(q.is_deleted, false) = false
                  AND lower(btrim(q."StockInNotifyId")) = lower(btrim(n."UserId"))
            ) THEN 30
            WHEN n."ReceiveQty" > 0 THEN 20
            ELSE 10
        END AS new_status
    FROM public.stockin_notify n
    WHERE COALESCE(n.is_deleted, false) = false
)
SELECT
    notice_code,
    old_status,
    new_status,
    CASE old_status
        WHEN 1 THEN '新建'
        WHEN 10 THEN '未到货'
        WHEN 20 THEN '到货待检'
        WHEN 30 THEN '已质检'
        WHEN 100 THEN '已入库'
        ELSE old_status::text
    END AS old_status_label,
    CASE new_status
        WHEN 10 THEN '未到货'
        WHEN 20 THEN '到货待检'
        WHEN 30 THEN '已质检'
        WHEN 100 THEN '已入库'
        ELSE new_status::text
    END AS new_status_label
FROM arrival_eval
WHERE old_status IS DISTINCT FROM new_status
ORDER BY
    CASE WHEN old_status = 30 AND new_status = 100 THEN 0 ELSE 1 END,
    notice_code;

-- ---------------------------------------------------------------------------
-- 2) 更新（确认 0b/0c 与预览无误后执行）
-- ---------------------------------------------------------------------------
BEGIN;

WITH arrival_eval AS (
    SELECT
        n."UserId" AS id,
        n."Status" AS old_status,
        CASE
            WHEN EXISTS (
                SELECT 1
                FROM public.stock_in si
                WHERE COALESCE(si.is_deleted, false) = false
                  AND si."Status" = 2
                  AND si."StockInType" <> 3
                  AND CASE
                      WHEN si."StockInType" IN (10, 20, 30, 40) THEN si."StockInType"
                      ELSE 10
                  END = CASE
                      WHEN n."StockInType" IN (10, 20, 30, 40) THEN n."StockInType"
                      ELSE 10
                  END
                  AND (
                      lower(btrim(si."SourceId")) = lower(btrim(n."UserId"))
                      OR EXISTS (
                          SELECT 1
                          FROM public.qcinfo q
                          WHERE COALESCE(q.is_deleted, false) = false
                            AND lower(btrim(q."StockInNotifyId")) = lower(btrim(n."UserId"))
                            AND (
                                (si."QCID" IS NOT NULL AND lower(btrim(si."QCID")) = lower(btrim(q."UserId")))
                                OR (q."StockInId" IS NOT NULL AND lower(btrim(q."StockInId")) = lower(btrim(si."StockInId")))
                            )
                      )
                  )
            ) THEN 100
            WHEN EXISTS (
                SELECT 1
                FROM public.qcinfo q
                WHERE COALESCE(q.is_deleted, false) = false
                  AND lower(btrim(q."StockInNotifyId")) = lower(btrim(n."UserId"))
            ) THEN 30
            WHEN n."ReceiveQty" > 0 THEN 20
            ELSE 10
        END AS new_status
    FROM public.stockin_notify n
    WHERE COALESCE(n.is_deleted, false) = false
),
arrival_changed AS (
    SELECT id, new_status
    FROM arrival_eval
    WHERE old_status IS DISTINCT FROM new_status
)
UPDATE public.stockin_notify n
SET
    "Status" = c.new_status,
    "ModifyTime" = NOW() AT TIME ZONE 'utc'
FROM arrival_changed c
WHERE n."UserId" = c.id;

COMMIT;

-- ---------------------------------------------------------------------------
-- 3) 校验：仍应为「已入库」却显示「已质检」（期望 0 行）
-- ---------------------------------------------------------------------------
SELECT
    n."NoticeCode" AS notice_code,
    n."Status" AS status,
    si."StockInCode" AS stock_in_code,
    si."Status" AS stock_in_status,
    si."StockInType" AS stock_in_type
FROM public.stockin_notify n
INNER JOIN public.stock_in si
    ON COALESCE(si.is_deleted, false) = false
   AND si."Status" = 2
   AND si."StockInType" <> 3
   AND CASE
       WHEN si."StockInType" IN (10, 20, 30, 40) THEN si."StockInType"
       ELSE 10
   END = CASE
       WHEN n."StockInType" IN (10, 20, 30, 40) THEN n."StockInType"
       ELSE 10
   END
   AND (
       lower(btrim(si."SourceId")) = lower(btrim(n."UserId"))
       OR EXISTS (
           SELECT 1
           FROM public.qcinfo q
           WHERE COALESCE(q.is_deleted, false) = false
             AND lower(btrim(q."StockInNotifyId")) = lower(btrim(n."UserId"))
             AND (
                 (si."QCID" IS NOT NULL AND lower(btrim(si."QCID")) = lower(btrim(q."UserId")))
                 OR (q."StockInId" IS NOT NULL AND lower(btrim(q."StockInId")) = lower(btrim(si."StockInId")))
             )
       )
   )
WHERE COALESCE(n.is_deleted, false) = false
  AND n."Status" = 30
ORDER BY n."NoticeCode";
