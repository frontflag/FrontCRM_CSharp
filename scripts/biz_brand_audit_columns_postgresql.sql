-- biz_brand：创建人/创建日期/审核状态/审核人/审核日期
-- 审核状态：1=待审核，2=已审核

ALTER TABLE public.biz_brand
  ADD COLUMN IF NOT EXISTS create_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS create_time timestamp with time zone NULL,
  ADD COLUMN IF NOT EXISTS audit_status smallint NULL,
  ADD COLUMN IF NOT EXISTS audit_by_user_id character varying(36) NULL,
  ADD COLUMN IF NOT EXISTS audit_time timestamp with time zone NULL;

COMMENT ON COLUMN public.biz_brand.create_by_user_id IS '创建人用户ID（关联 user.UserId）';
COMMENT ON COLUMN public.biz_brand.create_time IS '创建日期';
COMMENT ON COLUMN public.biz_brand.audit_status IS '审核状态：1待审核，2已审核';
COMMENT ON COLUMN public.biz_brand.audit_by_user_id IS '审核人用户ID（关联 user.UserId）';
COMMENT ON COLUMN public.biz_brand.audit_time IS '审核日期';

CREATE INDEX IF NOT EXISTS "IX_biz_brand_audit_status"
    ON public.biz_brand (audit_status);

CREATE INDEX IF NOT EXISTS "IX_biz_brand_create_time"
    ON public.biz_brand (create_time);

-- 历史数据：创建人/审核人=admin，创建/审核日期=当前时间，审核状态=已审核(2)
UPDATE public.biz_brand b
SET
  create_by_user_id = u."UserId",
  audit_by_user_id = u."UserId",
  create_time = CURRENT_TIMESTAMP,
  audit_time = CURRENT_TIMESTAMP,
  audit_status = 2
FROM (
  SELECT "UserId"
  FROM public."user"
  WHERE LOWER("UserName") = 'admin'
  LIMIT 1
) u
WHERE u."UserId" IS NOT NULL;

-- 验证
-- SELECT id, create_by_user_id, create_time, audit_status, audit_by_user_id, audit_time
-- FROM public.biz_brand ORDER BY id LIMIT 10;
