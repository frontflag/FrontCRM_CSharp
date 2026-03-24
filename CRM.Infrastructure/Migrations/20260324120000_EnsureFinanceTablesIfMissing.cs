using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 部分环境未执行早期迁移导致缺少 financereceipt 等表；待审批等接口查询时会报错 42P01。
    /// 本迁移用 IF NOT EXISTS 补齐财务核心表（与 20260319021703_AddVendorTables 一致）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324120000_EnsureFinanceTablesIfMissing")]
    public partial class EnsureFinanceTablesIfMissing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- financepayment
CREATE TABLE IF NOT EXISTS public.financepayment (
    ""FinancePaymentId"" character varying(36) NOT NULL,
    ""FinancePaymentCode"" character varying(16) NOT NULL,
    ""VendorId"" character varying(36) NOT NULL,
    ""VendorName"" character varying(200),
    ""Status"" smallint NOT NULL DEFAULT 0,
    ""PaymentAmountToBe"" numeric(18,2) NOT NULL,
    ""PaymentAmount"" numeric(18,2) NOT NULL,
    ""PaymentTotalAmount"" numeric(18,2) NOT NULL,
    ""PaymentCurrency"" smallint NOT NULL,
    ""PaymentDate"" timestamp with time zone,
    ""PaymentUserId"" character varying(36),
    ""PaymentMode"" smallint NOT NULL,
    ""Remark"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_financepayment"" PRIMARY KEY (""FinancePaymentId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_financepayment_FinancePaymentCode""
    ON public.financepayment (""FinancePaymentCode"");

-- financepurchaseinvoice
CREATE TABLE IF NOT EXISTS public.financepurchaseinvoice (
    ""FinancePurchaseInvoiceId"" character varying(36) NOT NULL,
    ""VendorId"" character varying(36) NOT NULL,
    ""VendorName"" character varying(200),
    ""InvoiceNo"" character varying(32),
    ""InvoiceAmount"" numeric(18,2) NOT NULL,
    ""BillAmount"" numeric(18,2) NOT NULL,
    ""TaxAmount"" numeric(18,2) NOT NULL,
    ""ExcludTaxAmount"" numeric(18,2) NOT NULL,
    ""InvoiceDate"" timestamp with time zone,
    ""ConfirmDate"" timestamp with time zone,
    ""ConfirmStatus"" smallint NOT NULL,
    ""RedInvoiceStatus"" smallint NOT NULL,
    ""Remark"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_financepurchaseinvoice"" PRIMARY KEY (""FinancePurchaseInvoiceId"")
);

-- financereceipt
CREATE TABLE IF NOT EXISTS public.financereceipt (
    ""FinanceReceiptId"" character varying(36) NOT NULL,
    ""FinanceReceiptCode"" character varying(16) NOT NULL,
    ""CustomerId"" character varying(36) NOT NULL,
    ""CustomerName"" character varying(200),
    ""SalesUserId"" character varying(36),
    ""PurchaseGroupId"" character varying(36),
    ""Status"" smallint NOT NULL DEFAULT 0,
    ""ReceiptAmount"" numeric(18,2) NOT NULL,
    ""ReceiptCurrency"" smallint NOT NULL,
    ""ReceiptDate"" timestamp with time zone,
    ""ReceiptUserId"" character varying(36),
    ""ReceiptMode"" smallint NOT NULL,
    ""ReceiptBankId"" character varying(36),
    ""Remark"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_financereceipt"" PRIMARY KEY (""FinanceReceiptId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_financereceipt_FinanceReceiptCode""
    ON public.financereceipt (""FinanceReceiptCode"");

-- financesellinvoice
CREATE TABLE IF NOT EXISTS public.financesellinvoice (
    ""FinanceSellInvoiceId"" character varying(36) NOT NULL,
    ""CustomerId"" character varying(36) NOT NULL,
    ""CustomerName"" character varying(200),
    ""InvoiceCode"" character varying(32),
    ""InvoiceNo"" character varying(64),
    ""InvoiceTotal"" numeric(18,2) NOT NULL,
    ""MakeInvoiceDate"" timestamp with time zone,
    ""ReceiveStatus"" smallint NOT NULL,
    ""ReceiveDone"" numeric(18,2) NOT NULL,
    ""ReceiveToBe"" numeric(18,2) NOT NULL,
    ""Currency"" smallint NOT NULL,
    ""Type"" smallint NOT NULL,
    ""InvoiceStatus"" smallint NOT NULL,
    ""SellInvoiceType"" smallint NOT NULL,
    ""Remark"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_financesellinvoice"" PRIMARY KEY (""FinanceSellInvoiceId"")
);

-- financepaymentitem
CREATE TABLE IF NOT EXISTS public.financepaymentitem (
    ""FinancePaymentItemId"" character varying(36) NOT NULL,
    ""FinancePaymentId"" character varying(36) NOT NULL,
    ""PurchaseOrderId"" character varying(36),
    ""PurchaseOrderItemId"" character varying(36),
    ""PaymentAmount"" numeric(18,2) NOT NULL,
    ""PaymentAmountToBe"" numeric(18,2) NOT NULL,
    ""ProductId"" character varying(36),
    ""PN"" character varying(64),
    ""Brand"" character varying(64),
    ""VerificationStatus"" smallint NOT NULL,
    ""VerificationDone"" numeric(18,2) NOT NULL,
    ""VerificationToBe"" numeric(18,2) NOT NULL,
    ""PaymentId"" character varying(36),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_financepaymentitem"" PRIMARY KEY (""FinancePaymentItemId"")
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_financepaymentitem_financepayment_FinancePaymentId') THEN
        ALTER TABLE public.financepaymentitem
            ADD CONSTRAINT ""FK_financepaymentitem_financepayment_FinancePaymentId""
            FOREIGN KEY (""FinancePaymentId"") REFERENCES public.financepayment (""FinancePaymentId"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_financepaymentitem_financepayment_PaymentId') THEN
        ALTER TABLE public.financepaymentitem
            ADD CONSTRAINT ""FK_financepaymentitem_financepayment_PaymentId""
            FOREIGN KEY (""PaymentId"") REFERENCES public.financepayment (""FinancePaymentId"");
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ""IX_financepaymentitem_FinancePaymentId""
    ON public.financepaymentitem (""FinancePaymentId"");

-- financepurchaseinvoiceitem
CREATE TABLE IF NOT EXISTS public.financepurchaseinvoiceitem (
    ""FinancePurchaseInvoiceItemId"" character varying(36) NOT NULL,
    ""FinancePurchaseInvoiceId"" character varying(36) NOT NULL,
    ""StockInId"" character varying(36),
    ""StockInCode"" character varying(32),
    ""PurchaseOrderCode"" character varying(32),
    ""StockInCost"" numeric(18,4) NOT NULL,
    ""BillCost"" numeric(18,4) NOT NULL,
    ""BillQty"" bigint NOT NULL,
    ""BillAmount"" numeric(18,2) NOT NULL,
    ""TaxRate"" numeric(18,4) NOT NULL,
    ""TaxAmount"" numeric(18,2) NOT NULL,
    ""ExcludTaxAmount"" numeric(18,2) NOT NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_financepurchaseinvoiceitem"" PRIMARY KEY (""FinancePurchaseInvoiceItemId"")
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname LIKE 'FK_financepurchaseinvoiceitem_financepurchaseinvoice%') THEN
        ALTER TABLE public.financepurchaseinvoiceitem
            ADD CONSTRAINT ""FK_financepurchaseinvoiceitem_financepurchaseinvoice_FinancePurchaseInvoiceId""
            FOREIGN KEY (""FinancePurchaseInvoiceId"") REFERENCES public.financepurchaseinvoice (""FinancePurchaseInvoiceId"") ON DELETE CASCADE;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ""IX_financepurchaseinvoiceitem_FinancePurchaseInvoiceId""
    ON public.financepurchaseinvoiceitem (""FinancePurchaseInvoiceId"");

-- financereceiptitem
CREATE TABLE IF NOT EXISTS public.financereceiptitem (
    ""FinanceReceiptItemId"" character varying(36) NOT NULL,
    ""FinanceReceiptId"" character varying(36) NOT NULL,
    ""SellOrderId"" character varying(36),
    ""SellOrderItemId"" character varying(36),
    ""FinanceSellInvoiceId"" character varying(36),
    ""FinanceSellInvoiceItemId"" character varying(36),
    ""ReceiptAmount"" numeric(18,2) NOT NULL,
    ""ReceiptConvertAmount"" numeric(18,2) NOT NULL,
    ""StockOutItemId"" character varying(36),
    ""ProductId"" character varying(36),
    ""PN"" character varying(64),
    ""Brand"" character varying(64),
    ""VerificationStatus"" smallint NOT NULL,
    ""ReceiptId"" character varying(36),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_financereceiptitem"" PRIMARY KEY (""FinanceReceiptItemId"")
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_financereceiptitem_financereceipt_FinanceReceiptId') THEN
        ALTER TABLE public.financereceiptitem
            ADD CONSTRAINT ""FK_financereceiptitem_financereceipt_FinanceReceiptId""
            FOREIGN KEY (""FinanceReceiptId"") REFERENCES public.financereceipt (""FinanceReceiptId"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_financereceiptitem_financereceipt_ReceiptId') THEN
        ALTER TABLE public.financereceiptitem
            ADD CONSTRAINT ""FK_financereceiptitem_financereceipt_ReceiptId""
            FOREIGN KEY (""ReceiptId"") REFERENCES public.financereceipt (""FinanceReceiptId"");
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ""IX_financereceiptitem_FinanceReceiptId""
    ON public.financereceiptitem (""FinanceReceiptId"");

-- sellinvoiceitem
CREATE TABLE IF NOT EXISTS public.sellinvoiceitem (
    ""SellInvoiceItemId"" character varying(36) NOT NULL,
    ""FinanceSellInvoiceId"" character varying(36) NOT NULL,
    ""InvoiceTotal"" numeric(18,2) NOT NULL,
    ""TaxRate"" numeric(18,4) NOT NULL,
    ""ValueAddedTax"" numeric(18,2) NOT NULL,
    ""TaxFreeTotal"" numeric(18,2) NOT NULL,
    ""Price"" numeric(18,4) NOT NULL,
    ""Qty"" bigint NOT NULL,
    ""StockOutItemId"" character varying(36),
    ""Currency"" smallint NOT NULL,
    ""ReceiveStatus"" smallint NOT NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_sellinvoiceitem"" PRIMARY KEY (""SellInvoiceItemId"")
);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_sellinvoiceitem_financesellinvoice_FinanceSellInvoiceId') THEN
        ALTER TABLE public.sellinvoiceitem
            ADD CONSTRAINT ""FK_sellinvoiceitem_financesellinvoice_FinanceSellInvoiceId""
            FOREIGN KEY (""FinanceSellInvoiceId"") REFERENCES public.financesellinvoice (""FinanceSellInvoiceId"") ON DELETE CASCADE;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ""IX_sellinvoiceitem_FinanceSellInvoiceId""
    ON public.sellinvoiceitem (""FinanceSellInvoiceId"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 不删除表，避免误删生产数据；如需回滚请手工处理
        }
    }
}
