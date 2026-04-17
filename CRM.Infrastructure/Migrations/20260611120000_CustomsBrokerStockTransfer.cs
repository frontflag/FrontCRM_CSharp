using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 报关公司、报关主/明细、移库主/明细；<c>stockledger</c> 扩列；流水号模块 <c>StockTransfer</c>、<c>CustomsDeclaration</c>。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260611120000_CustomsBrokerStockTransfer")]
    public partial class CustomsBrokerStockTransfer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.customs_broker (
  ""Id"" character varying(36) NOT NULL,
  ""BrokerCode"" character varying(32) NOT NULL,
  ""Name"" character varying(200) NOT NULL,
  ""Status"" smallint NOT NULL DEFAULT 1,
  ""Remark"" character varying(500) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  ""create_by_user_id"" character varying(36) NULL,
  ""modify_by_user_id"" character varying(36) NULL,
  CONSTRAINT ""PK_customs_broker"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_broker_BrokerCode"" ON public.customs_broker (""BrokerCode"");

CREATE TABLE IF NOT EXISTS public.customs_declaration (
  ""CustomsDeclarationId"" character varying(36) NOT NULL,
  ""DeclarationCode"" character varying(32) NOT NULL,
  ""StockOutRequestId"" character varying(36) NOT NULL,
  ""CustomsBrokerId"" character varying(36) NOT NULL,
  ""DeclarationType"" smallint NOT NULL DEFAULT 1,
  ""InternalStatus"" smallint NOT NULL DEFAULT 1,
  ""CustomsClearanceStatus"" smallint NOT NULL DEFAULT 0,
  ""DeclareDate"" timestamp with time zone NOT NULL,
  ""ExchangeRate"" numeric(18,6) NOT NULL DEFAULT 1,
  ""TotalTaxAmount"" numeric(18,2) NOT NULL DEFAULT 0,
  ""FromWarehouseId"" character varying(36) NOT NULL,
  ""ToWarehouseId"" character varying(36) NOT NULL,
  ""Remark"" character varying(500) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  ""create_by_user_id"" character varying(36) NULL,
  ""modify_by_user_id"" character varying(36) NULL,
  CONSTRAINT ""PK_customs_declaration"" PRIMARY KEY (""CustomsDeclarationId""),
  CONSTRAINT ""FK_customs_declaration_broker"" FOREIGN KEY (""CustomsBrokerId"") REFERENCES public.customs_broker (""Id"") ON DELETE RESTRICT,
  CONSTRAINT ""FK_customs_declaration_sor"" FOREIGN KEY (""StockOutRequestId"") REFERENCES public.stockoutrequest (""UserId"") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_DeclarationCode"" ON public.customs_declaration (""DeclarationCode"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_StockOutRequestId"" ON public.customs_declaration (""StockOutRequestId"");

CREATE TABLE IF NOT EXISTS public.customs_declaration_item (
  ""CustomsDeclarationItemId"" character varying(36) NOT NULL,
  ""DeclarationId"" character varying(36) NOT NULL,
  ""LineNo"" integer NOT NULL DEFAULT 0,
  ""SourceStockItemId"" character varying(36) NOT NULL,
  ""StockOutRequestId"" character varying(36) NOT NULL,
  ""MaterialId"" character varying(36) NOT NULL,
  ""HsCode"" character varying(32) NULL,
  ""DeclareQty"" integer NOT NULL,
  ""DeclareUnitPrice"" numeric(18,6) NOT NULL DEFAULT 0,
  ""DutyAmount"" numeric(18,2) NOT NULL DEFAULT 0,
  ""VatAmount"" numeric(18,2) NOT NULL DEFAULT 0,
  ""CustomsPaymentGoods"" numeric(18,2) NOT NULL DEFAULT 0,
  ""CustomsAgencyFee"" numeric(18,2) NOT NULL DEFAULT 0,
  ""OtherFee"" numeric(18,2) NOT NULL DEFAULT 0,
  ""InspectionFee"" numeric(18,2) NOT NULL DEFAULT 0,
  ""TotalValueTax"" numeric(18,2) NOT NULL DEFAULT 0,
  ""TaxIncludedUnitPrice"" numeric(18,6) NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  CONSTRAINT ""PK_customs_declaration_item"" PRIMARY KEY (""CustomsDeclarationItemId""),
  CONSTRAINT ""FK_cdi_declaration"" FOREIGN KEY (""DeclarationId"") REFERENCES public.customs_declaration (""CustomsDeclarationId"") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ""IX_cdi_DeclarationId"" ON public.customs_declaration_item (""DeclarationId"");

CREATE TABLE IF NOT EXISTS public.stocktransfer (
  ""StockTransferId"" character varying(36) NOT NULL,
  ""TransferCode"" character varying(32) NOT NULL,
  ""BizScene"" character varying(32) NOT NULL,
  ""CustomsDeclarationId"" character varying(36) NOT NULL,
  ""FromWarehouseId"" character varying(36) NOT NULL,
  ""ToWarehouseId"" character varying(36) NOT NULL,
  ""Status"" smallint NOT NULL DEFAULT 2,
  ""ConfirmedTime"" timestamp with time zone NULL,
  ""ConfirmedByUserId"" character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  ""create_by_user_id"" character varying(36) NULL,
  ""modify_by_user_id"" character varying(36) NULL,
  CONSTRAINT ""PK_stocktransfer"" PRIMARY KEY (""StockTransferId""),
  CONSTRAINT ""FK_stocktransfer_declaration"" FOREIGN KEY (""CustomsDeclarationId"") REFERENCES public.customs_declaration (""CustomsDeclarationId"") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stocktransfer_TransferCode"" ON public.stocktransfer (""TransferCode"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stocktransfer_CustomsDeclarationId"" ON public.stocktransfer (""CustomsDeclarationId"");

CREATE TABLE IF NOT EXISTS public.stocktransferitem (
  ""StockTransferItemId"" character varying(36) NOT NULL,
  ""StockTransferId"" character varying(36) NOT NULL,
  ""SourceStockItemId"" character varying(36) NOT NULL,
  ""CustomsDeclarationItemId"" character varying(36) NOT NULL,
  ""StockOutRequestId"" character varying(36) NOT NULL,
  ""Qty"" integer NOT NULL,
  ""TargetStockItemId"" character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  CONSTRAINT ""PK_stocktransferitem"" PRIMARY KEY (""StockTransferItemId""),
  CONSTRAINT ""FK_sti_transfer"" FOREIGN KEY (""StockTransferId"") REFERENCES public.stocktransfer (""StockTransferId"") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ""IX_sti_StockTransferId"" ON public.stocktransferitem (""StockTransferId"");

ALTER TABLE IF EXISTS public.stockledger
  ADD COLUMN IF NOT EXISTS ""from_warehouse_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockledger
  ADD COLUMN IF NOT EXISTS ""to_warehouse_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockledger
  ADD COLUMN IF NOT EXISTS ""create_by_user_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockledger
  ADD COLUMN IF NOT EXISTS ""customs_declaration_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockledger
  ADD COLUMN IF NOT EXISTS ""stock_transfer_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockledger
  ADD COLUMN IF NOT EXISTS ""source_stock_item_id"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockledger
  ADD COLUMN IF NOT EXISTS ""target_stock_item_id"" character varying(36) NULL;

DO $serial$
DECLARE nid int;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'StockTransfer') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'StockTransfer', '移库单', 'STF', 5, -1, false, false, timezone('utc', now()));
  END IF;
  IF NOT EXISTS (SELECT 1 FROM public.sys_serial_number WHERE ""ModuleCode"" = 'CustomsDeclaration') THEN
    SELECT COALESCE(MAX(""Id""), 0) + 1 INTO nid FROM public.sys_serial_number;
    INSERT INTO public.sys_serial_number (""Id"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""CurrentSequence"", ""ResetByYear"", ""ResetByMonth"", ""CreateTime"")
    VALUES (nid, 'CustomsDeclaration', '报关单', 'CDS', 5, -1, false, false, timezone('utc', now()));
  END IF;
END $serial$;
");

            migrationBuilder.Sql(@"
COMMENT ON TABLE public.customs_broker IS '报关公司主数据';
COMMENT ON TABLE public.customs_declaration IS '报关主单（与出库通知、移库单 1:1:1）';
COMMENT ON TABLE public.customs_declaration_item IS '报关明细';
COMMENT ON TABLE public.stocktransfer IS '库存移库单（一期：报关进口迁库）';
COMMENT ON TABLE public.stocktransferitem IS '移库明细';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.stocktransferitem;
DROP TABLE IF EXISTS public.stocktransfer;
DROP TABLE IF EXISTS public.customs_declaration_item;
DROP TABLE IF EXISTS public.customs_declaration;
DROP TABLE IF EXISTS public.customs_broker;

DELETE FROM public.sys_serial_number WHERE ""ModuleCode"" IN ('StockTransfer', 'CustomsDeclaration');

ALTER TABLE IF EXISTS public.stockledger DROP COLUMN IF EXISTS ""target_stock_item_id"";
ALTER TABLE IF EXISTS public.stockledger DROP COLUMN IF EXISTS ""source_stock_item_id"";
ALTER TABLE IF EXISTS public.stockledger DROP COLUMN IF EXISTS ""stock_transfer_id"";
ALTER TABLE IF EXISTS public.stockledger DROP COLUMN IF EXISTS ""customs_declaration_id"";
ALTER TABLE IF EXISTS public.stockledger DROP COLUMN IF EXISTS ""create_by_user_id"";
ALTER TABLE IF EXISTS public.stockledger DROP COLUMN IF EXISTS ""to_warehouse_id"";
ALTER TABLE IF EXISTS public.stockledger DROP COLUMN IF EXISTS ""from_warehouse_id"";
");
        }
    }
}
