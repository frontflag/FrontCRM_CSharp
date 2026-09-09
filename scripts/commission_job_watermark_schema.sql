-- 提成 Job 水位（单行）。API 启动不改库，请手工执行后重启。
CREATE TABLE IF NOT EXISTS public.commission_job_watermark (
  id character varying(36) NOT NULL,
  dynamic_job_date date NULL,
  lock_term character(6) NULL,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NULL,
  CONSTRAINT "PK_commission_job_watermark" PRIMARY KEY (id)
);

COMMENT ON TABLE public.commission_job_watermark IS '提成 Job 水位：动态计算日与最近锁定账期';

INSERT INTO public.commission_job_watermark (id, created_at)
VALUES ('c2000000-0000-4000-8000-000000000011', NOW())
ON CONFLICT (id) DO NOTHING;
