-- =============================================================================
-- 货代付款 P0 — PostgreSQL DDL
-- 与 Migration 20260808120000 / 20260808130000 对齐
-- 业务：收款单打标「货代付款」→ 审核通过后进入台账 → 线下转付货代并登记付款明细
-- 依赖：financereceipt、sys_serial_number 已存在
-- 执行前：备份数据库
-- =============================================================================

-- ---------------------------------------------------------------------------
-- 1. 货代公司主数据（独立于客户/供应商）
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.freight_forwarder_company (
  "Id" character varying(36) NOT NULL,
  "CompanyCode" character varying(32) NOT NULL,
  "cname" character varying(200) NOT NULL,
  "ename" character varying(200) NULL,
  "Status" smallint NOT NULL DEFAULT 1,
  "Remark" character varying(500) NULL,
  "is_deleted" boolean NOT NULL DEFAULT false,
  "deleted_at" timestamp with time zone NULL,
  "deleted_by_user_id" character varying(36) NULL,
  "create_by_user_id" character varying(36) NULL,
  "modify_by_user_id" character varying(36) NULL,
  "CreateTime" timestamp with time zone NOT NULL,
  "ModifyTime" timestamp with time zone NULL,
  CONSTRAINT "PK_freight_forwarder_company" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_freight_forwarder_company_CompanyCode"
  ON public.freight_forwarder_company ("CompanyCode")
  WHERE "is_deleted" = false;

COMMENT ON TABLE public.freight_forwarder_company IS '货代公司主数据；与客户、供应商无关联，在「货代公司管理」维护';
COMMENT ON COLUMN public.freight_forwarder_company."Id" IS '主键（GUID）';
COMMENT ON COLUMN public.freight_forwarder_company."CompanyCode" IS '货代公司编号；系统流水号生成，前缀 FFC（sys_serial_number.FreightForwarderCompany）';
COMMENT ON COLUMN public.freight_forwarder_company."cname" IS '货代公司中文名称';
COMMENT ON COLUMN public.freight_forwarder_company."ename" IS '货代公司英文名称（可选）';
COMMENT ON COLUMN public.freight_forwarder_company."Status" IS '启用状态：1=启用（下拉可选），0=停用';
COMMENT ON COLUMN public.freight_forwarder_company."Remark" IS '备注';
COMMENT ON COLUMN public.freight_forwarder_company."is_deleted" IS '软删除标记；true 时不在业务下拉与默认列表中出现';
COMMENT ON COLUMN public.freight_forwarder_company."deleted_at" IS '软删除时间（UTC）';
COMMENT ON COLUMN public.freight_forwarder_company."deleted_by_user_id" IS '执行软删除的操作人用户 ID';
COMMENT ON COLUMN public.freight_forwarder_company."create_by_user_id" IS '创建人用户 ID';
COMMENT ON COLUMN public.freight_forwarder_company."modify_by_user_id" IS '最后修改人用户 ID';
COMMENT ON COLUMN public.freight_forwarder_company."CreateTime" IS '创建时间（UTC）';
COMMENT ON COLUMN public.freight_forwarder_company."ModifyTime" IS '最后修改时间（UTC）';

-- ---------------------------------------------------------------------------
-- 2. 货代公司收款银行账户
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.freight_forwarder_company_bank (
  "Id" character varying(36) NOT NULL,
  "FreightForwarderCompanyId" character varying(36) NOT NULL,
  "BankName" character varying(200) NOT NULL,
  "AccountName" character varying(200) NULL,
  "AccountNo" character varying(64) NULL,
  "Currency" smallint NOT NULL DEFAULT 1,
  "IsDefault" boolean NOT NULL DEFAULT false,
  "IsDisabled" boolean NOT NULL DEFAULT false,
  is_deleted boolean NOT NULL DEFAULT false,
  "create_by_user_id" character varying(36) NULL,
  "modify_by_user_id" character varying(36) NULL,
  "CreateTime" timestamp with time zone NOT NULL,
  "ModifyTime" timestamp with time zone NULL,
  CONSTRAINT "PK_freight_forwarder_company_bank" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_freight_forwarder_company_bank_company"
  ON public.freight_forwarder_company_bank ("FreightForwarderCompanyId");

COMMENT ON TABLE public.freight_forwarder_company_bank IS '货代公司收款银行账户；登记货代付款时选择货代收款银行';
COMMENT ON COLUMN public.freight_forwarder_company_bank."Id" IS '主键（GUID）';
COMMENT ON COLUMN public.freight_forwarder_company_bank."FreightForwarderCompanyId" IS '所属货代公司主键（freight_forwarder_company.Id）';
COMMENT ON COLUMN public.freight_forwarder_company_bank."BankName" IS '开户银行名称';
COMMENT ON COLUMN public.freight_forwarder_company_bank."AccountName" IS '银行账户户名';
COMMENT ON COLUMN public.freight_forwarder_company_bank."AccountNo" IS '银行账号';
COMMENT ON COLUMN public.freight_forwarder_company_bank."Currency" IS '账户币别：1=人民币，2=美元，3=欧元';
COMMENT ON COLUMN public.freight_forwarder_company_bank."IsDefault" IS '是否默认收款账户；同一货代公司仅建议一条为 true';
COMMENT ON COLUMN public.freight_forwarder_company_bank."IsDisabled" IS '是否停用；true 时付款弹窗不可选';
COMMENT ON COLUMN public.freight_forwarder_company_bank.is_deleted IS '软删除；默认查询排除';
COMMENT ON COLUMN public.freight_forwarder_company_bank."create_by_user_id" IS '创建人用户 ID';
COMMENT ON COLUMN public.freight_forwarder_company_bank."modify_by_user_id" IS '最后修改人用户 ID';
COMMENT ON COLUMN public.freight_forwarder_company_bank."CreateTime" IS '创建时间（UTC）';
COMMENT ON COLUMN public.freight_forwarder_company_bank."ModifyTime" IS '最后修改时间（UTC）';

-- ---------------------------------------------------------------------------
-- 3. 货代付款明细（无审核，线下付款后系统记账）
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.finance_freight_forwarder_payment (
  "FinanceFfPaymentId" character varying(36) NOT NULL,
  "FinanceReceiptId" character varying(36) NOT NULL,
  "FreightForwarderCompanyId" character varying(36) NOT NULL,
  "PaymentAmount" numeric(18,2) NOT NULL DEFAULT 0,
  "PaymentCurrency" smallint NOT NULL DEFAULT 1,
  "PaymentMode" smallint NOT NULL DEFAULT 1,
  "CompanyBankId" character varying(36) NULL,
  "FfCompanyBankId" character varying(36) NULL,
  "BankSlipNo" character varying(100) NULL,
  "PaymentDate" timestamp with time zone NULL,
  "PaymentUserId" character varying(36) NULL,
  "Remark" character varying(500) NULL,
  "is_deleted" boolean NOT NULL DEFAULT false,
  "create_by_user_id" character varying(36) NULL,
  "modify_by_user_id" character varying(36) NULL,
  "CreateTime" timestamp with time zone NOT NULL,
  "ModifyTime" timestamp with time zone NULL,
  CONSTRAINT "PK_finance_freight_forwarder_payment" PRIMARY KEY ("FinanceFfPaymentId")
);

CREATE INDEX IF NOT EXISTS "IX_finance_ff_payment_receipt"
  ON public.finance_freight_forwarder_payment ("FinanceReceiptId")
  WHERE "is_deleted" = false;

COMMENT ON TABLE public.finance_freight_forwarder_payment IS '货代付款明细；一笔收款可多次部分付款，汇总计入台账「已付」';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."FinanceFfPaymentId" IS '付款明细主键（GUID）';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."FinanceReceiptId" IS '关联收款单主键（financereceipt.FinanceReceiptId）；台账以收款单为维度';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."FreightForwarderCompanyId" IS '货代公司主键；与收款单上 freight_forwarder_company_id 一致，冗余便于历史追溯';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."PaymentAmount" IS '本次向货代付款金额；单笔须大于 0 且不超过收款单待付余额';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."PaymentCurrency" IS '付款币别：1=人民币，2=美元，3=欧元；默认与收款单 ReceiptCurrency 一致';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."PaymentMode" IS '付款方式：1=银行转账，2=现金，3=支票，4=承兑汇票';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."CompanyBankId" IS '公司付款银行账户 ID（company_bankinfo.Id）；我方出款账户';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."FfCompanyBankId" IS '货代收款银行账户 ID（freight_forwarder_company_bank.Id）';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."BankSlipNo" IS '银行水单号；线下付款凭证编号';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."PaymentDate" IS '付款日期（UTC 存储）；实际转付货代日期';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."PaymentUserId" IS '登记付款的操作人用户 ID（付款人）';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."Remark" IS '付款备注';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."is_deleted" IS '软删除标记；true 时不参与已付金额汇总';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."create_by_user_id" IS '创建人用户 ID';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."modify_by_user_id" IS '最后修改人用户 ID';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."CreateTime" IS '记录创建时间（UTC）';
COMMENT ON COLUMN public.finance_freight_forwarder_payment."ModifyTime" IS '最后修改时间（UTC）';

-- ---------------------------------------------------------------------------
-- 4. 收款单扩展：货代付款打标
-- ---------------------------------------------------------------------------
ALTER TABLE public.financereceipt
  ADD COLUMN IF NOT EXISTS is_freight_forwarder_payment boolean NOT NULL DEFAULT false;
ALTER TABLE public.financereceipt
  ADD COLUMN IF NOT EXISTS freight_forwarder_company_id character varying(36) NULL;

COMMENT ON COLUMN public.financereceipt.is_freight_forwarder_payment IS '是否货代付款收款：true 表示该笔客户收款需转付货代；与预收款（receipt_purpose=20）互斥';
COMMENT ON COLUMN public.financereceipt.freight_forwarder_company_id IS '货代公司主键（可选）；收款时可空，首次登记货代付款前必填；审核通过且为 true 时进入货代付款台账';

-- ---------------------------------------------------------------------------
-- 5. 流水号：货代公司编号 FFC
-- ---------------------------------------------------------------------------
DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE "ModuleCode" = 'FreightForwarderCompany') THEN
    SELECT COALESCE(MAX("Id"), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number ("Id", "ModuleCode", "ModuleName", "Prefix", "SequenceLength", "CurrentSequence", "ResetByYear", "ResetByMonth", "CreateTime")
    VALUES (nid, 'FreightForwarderCompany', '货代公司', 'FFC', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;
