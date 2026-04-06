-- 与 CRM.Core.Models.Finance.PaymentRequest + BaseEntity 对齐；库中无此表时执行一次即可消除 42P01。
-- 用法：psql -f create_paymentrequest_table_if_not_exists.sql  或在客户端中执行。

CREATE TABLE IF NOT EXISTS public.paymentrequest (
    "PaymentRequestId" character varying(36) NOT NULL,
    "RequestCode" character varying(50) NOT NULL,
    "PurchaseOrderId" character varying(36),
    "VendorId" character varying(36) NOT NULL,
    "RequestUserId" character varying(36) NOT NULL,
    "RequestDate" timestamp with time zone NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Currency" smallint NOT NULL DEFAULT 1,
    "PaymentMethod" smallint NOT NULL,
    "BankAccount" character varying(50),
    "BankName" character varying(100),
    "Remark" character varying(500),
    "Status" smallint NOT NULL DEFAULT 0,
    create_by_user_id character varying(36),
    modify_by_user_id character varying(36),
    "CreateTime" timestamp with time zone NOT NULL,
    "CreateUserId" bigint,
    "ModifyTime" timestamp with time zone,
    "ModifyUserId" bigint,
    CONSTRAINT "PK_paymentrequest" PRIMARY KEY ("PaymentRequestId")
);
