-- 提成计算全局参数（单行）。API 启动不改库，请手工执行后重启。
CREATE TABLE IF NOT EXISTS public.commission_calc_setting (
  id character varying(36) NOT NULL,
  receipt_writeoff_delay_days integer NOT NULL DEFAULT 0,
  created_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
  updated_by character varying(36) NULL,
  CONSTRAINT "PK_commission_calc_setting" PRIMARY KEY (id),
  CONSTRAINT "CK_commission_calc_setting_delay_days"
    CHECK (receipt_writeoff_delay_days BETWEEN 0 AND 3650)
);

COMMENT ON TABLE public.commission_calc_setting IS '提成计算参数：单行；收款核销延期提成天数业务/采购共用';
COMMENT ON COLUMN public.commission_calc_setting.receipt_writeoff_delay_days IS '提成计算日减收款核销日须大于该整数（上海日历日）';

INSERT INTO public.commission_calc_setting (id, receipt_writeoff_delay_days, created_at, updated_at)
VALUES ('c2000000-0000-4000-8000-000000000010', 0, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;
