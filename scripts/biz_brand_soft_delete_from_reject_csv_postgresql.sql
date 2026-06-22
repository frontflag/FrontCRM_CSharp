-- biz_brand：按 tm_pn_brand_审核不通过.csv 软删除「已审核」品牌
-- 匹配规则：brand_e_name、brand_c_name、standard_brand 三字段 TRIM 后完全一致
-- 仅处理：audit_status = 2（已审核）且 is_deleted = false
--
-- CSV 列：brand_e_name, brand_c_name, standard_brand
-- 文件：tm_pn_brand_审核不通过.csv
--
-- DBeaver 推荐流程：
--   1) 执行「步骤 1」建暂存表
--   2) 右键暂存表 → 导入数据 → 选择 CSV（首行表头，UTF-8）
--   3) 执行「步骤 2」预览
--   4) 确认无误后执行「步骤 3」软删除
--   5) 执行「步骤 4」核对

-- ========== 步骤 1：暂存表（可重复执行）==========
-- 注意：DBeaver「导入数据」若未先建表，会默认 varchar(50)，超过 50 字符会报错。
--       必须先执行本段 CREATE，或在导入向导里把三列长度改为 200/200/300。
DROP TABLE IF EXISTS public._staging_tm_pn_brand_rejected;

CREATE TABLE public._staging_tm_pn_brand_rejected (
    brand_e_name   character varying(200) NOT NULL DEFAULT '',
    brand_c_name   character varying(200) NOT NULL DEFAULT '',
    standard_brand character varying(300) NOT NULL DEFAULT ''
);

COMMENT ON TABLE public._staging_tm_pn_brand_rejected IS
  'tm_pn_brand_审核不通过.csv 导入暂存（用完可 DROP）';

-- 若已导入时报错「值太长了(50)」，先扩列再重新导入：
-- ALTER TABLE public._staging_tm_pn_brand_rejected
--   ALTER COLUMN brand_e_name   TYPE character varying(200),
--   ALTER COLUMN brand_c_name   TYPE character varying(200),
--   ALTER COLUMN standard_brand TYPE character varying(300);
-- TRUNCATE public._staging_tm_pn_brand_rejected;
-- 然后重新导入 CSV（编码选 GBK 或 UTF-8，以能正常显示中文为准）

-- 若 PostgreSQL 服务端能直接读 CSV 文件，可用 COPY（路径改为服务器上的实际路径）：
-- COPY public._staging_tm_pn_brand_rejected (brand_e_name, brand_c_name, standard_brand)
-- FROM 'D:/MyProject/FrontCRM_文档/数据库备份/品牌库/tm_pn_brand_审核不通过.csv'
-- WITH (FORMAT csv, HEADER true, ENCODING 'UTF8');

-- 导入后核对行数（约 1193 行，不含表头）
-- SELECT COUNT(*) FROM public._staging_tm_pn_brand_rejected;

-- CSV 内去重（三字段完全相同只保留一条，避免 JOIN 重复更新）
CREATE OR REPLACE VIEW public._v_tm_pn_brand_rejected_distinct AS
SELECT DISTINCT
    TRIM(COALESCE(brand_e_name, ''))   AS brand_e_name,
    TRIM(COALESCE(brand_c_name, ''))   AS brand_c_name,
    TRIM(COALESCE(standard_brand, '')) AS standard_brand
FROM public._staging_tm_pn_brand_rejected
WHERE TRIM(COALESCE(brand_e_name, '')) <> ''
   OR TRIM(COALESCE(brand_c_name, '')) <> ''
   OR TRIM(COALESCE(standard_brand, '')) <> '';

-- ========== 步骤 2：预览 — 将被软删除的 biz_brand ==========
SELECT
    b.id,
    b.brand_e_name,
    b.brand_c_name,
    b.standard_brand,
    b.audit_status,
    b.is_deleted,
    b.remark
FROM public.biz_brand b
INNER JOIN public._v_tm_pn_brand_rejected_distinct s
    ON TRIM(COALESCE(b.brand_e_name, ''))   = s.brand_e_name
   AND TRIM(COALESCE(b.brand_c_name, ''))   = s.brand_c_name
   AND TRIM(COALESCE(b.standard_brand, '')) = s.standard_brand
WHERE b.audit_status = 2
  AND b.is_deleted = false
ORDER BY b.id;

-- 预览统计
SELECT
    (SELECT COUNT(*) FROM public._v_tm_pn_brand_rejected_distinct) AS csv_distinct_rows,
    (
        SELECT COUNT(*)
        FROM public.biz_brand b
        INNER JOIN public._v_tm_pn_brand_rejected_distinct s
            ON TRIM(COALESCE(b.brand_e_name, ''))   = s.brand_e_name
           AND TRIM(COALESCE(b.brand_c_name, ''))   = s.brand_c_name
           AND TRIM(COALESCE(b.standard_brand, '')) = s.standard_brand
        WHERE b.audit_status = 2
          AND b.is_deleted = false
    ) AS will_soft_delete_count,
    (
        SELECT COUNT(*)
        FROM public._v_tm_pn_brand_rejected_distinct s
        WHERE NOT EXISTS (
            SELECT 1
            FROM public.biz_brand b
            WHERE TRIM(COALESCE(b.brand_e_name, ''))   = s.brand_e_name
              AND TRIM(COALESCE(b.brand_c_name, ''))   = s.brand_c_name
              AND TRIM(COALESCE(b.standard_brand, '')) = s.standard_brand
              AND b.audit_status = 2
              AND b.is_deleted = false
        )
    ) AS csv_rows_no_approved_match;

-- CSV 有、但库中无「已审核且未删」完全匹配（供排查）
SELECT s.*
FROM public._v_tm_pn_brand_rejected_distinct s
WHERE NOT EXISTS (
    SELECT 1
    FROM public.biz_brand b
    WHERE TRIM(COALESCE(b.brand_e_name, ''))   = s.brand_e_name
      AND TRIM(COALESCE(b.brand_c_name, ''))   = s.brand_c_name
      AND TRIM(COALESCE(b.standard_brand, '')) = s.standard_brand
      AND b.audit_status = 2
      AND b.is_deleted = false
)
ORDER BY s.standard_brand
LIMIT 50;

-- ========== 步骤 3：软删除（确认预览后再执行）==========
BEGIN;

UPDATE public.biz_brand AS b
SET
    is_deleted = true,
    deleted_at = NOW(),
    deleted_by_user_id = NULL
FROM public._v_tm_pn_brand_rejected_distinct AS s
WHERE b.audit_status = 2
  AND b.is_deleted = false
  AND TRIM(COALESCE(b.brand_e_name, ''))   = s.brand_e_name
  AND TRIM(COALESCE(b.brand_c_name, ''))   = s.brand_c_name
  AND TRIM(COALESCE(b.standard_brand, '')) = s.standard_brand;

-- 查看本事务影响行数后 COMMIT 或 ROLLBACK
-- COMMIT;
-- ROLLBACK;

-- ========== 步骤 4：核对 ==========
-- SELECT COUNT(*) AS approved_active
-- FROM public.biz_brand
-- WHERE audit_status = 2 AND is_deleted = false;
--
-- SELECT COUNT(*) AS soft_deleted_today
-- FROM public.biz_brand
-- WHERE is_deleted = true
--   AND deleted_at >= CURRENT_DATE;

-- 用完可清理暂存表
-- DROP TABLE IF EXISTS public._staging_tm_pn_brand_rejected;
-- DROP VIEW IF EXISTS public._v_tm_pn_brand_rejected_distinct;
