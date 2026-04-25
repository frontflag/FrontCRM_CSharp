using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 报关移库表重命名为 <c>stocktransfer_customers</c> / <c>stocktransfer_item_customers</c>；
    /// 新建手工移库表 <c>stocktransfer_manual</c> / <c>stocktransfer_item_manual</c>（字段后续迭代可扩展）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260706120000_RenameStockTransferCustomersAndManualTables")]
    public partial class RenameStockTransferCustomersAndManualTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $body$
BEGIN
  IF to_regclass('public.stocktransfer') IS NOT NULL
     AND to_regclass('public.stocktransfer_customers') IS NULL THEN
    ALTER TABLE public.stocktransfer RENAME TO stocktransfer_customers;
  END IF;

  IF to_regclass('public.stocktransferitem') IS NOT NULL
     AND to_regclass('public.stocktransfer_item_customers') IS NULL THEN
    ALTER TABLE public.stocktransferitem RENAME TO stocktransfer_item_customers;
  END IF;
END
$body$;

CREATE TABLE IF NOT EXISTS public.stocktransfer_manual (
  ""StockTransferManualId"" character varying(36) NOT NULL,
  ""TransferCode"" character varying(32) NOT NULL,
  ""FromWarehouseId"" character varying(36) NOT NULL,
  ""ToWarehouseId"" character varying(36) NOT NULL,
  ""Status"" smallint NOT NULL DEFAULT 0,
  ""Remark"" character varying(500) NULL,
  ""ConfirmedTime"" timestamp with time zone NULL,
  ""ConfirmedByUserId"" character varying(36) NULL,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  ""create_by_user_id"" character varying(36) NULL,
  ""modify_by_user_id"" character varying(36) NULL,
  CONSTRAINT ""PK_stocktransfer_manual"" PRIMARY KEY (""StockTransferManualId""),
  CONSTRAINT ""IX_stocktransfer_manual_TransferCode"" UNIQUE (""TransferCode"")
);

CREATE TABLE IF NOT EXISTS public.stocktransfer_item_manual (
  ""StockTransferItemManualId"" character varying(36) NOT NULL,
  ""StockTransferManualId"" character varying(36) NOT NULL,
  ""SourceStockItemId"" character varying(36) NOT NULL,
  ""TargetStockItemId"" character varying(36) NULL,
  ""Qty"" integer NOT NULL DEFAULT 0,
  ""CreateTime"" timestamp with time zone NOT NULL,
  ""ModifyTime"" timestamp with time zone NULL,
  ""CreateUserId"" bigint NULL,
  ""ModifyUserId"" bigint NULL,
  CONSTRAINT ""PK_stocktransfer_item_manual"" PRIMARY KEY (""StockTransferItemManualId""),
  CONSTRAINT ""FK_stim_manual"" FOREIGN KEY (""StockTransferManualId"") REFERENCES public.stocktransfer_manual (""StockTransferManualId"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_stim_StockTransferManualId"" ON public.stocktransfer_item_manual (""StockTransferManualId"");

DO $cmt$
BEGIN
  IF to_regclass('public.stocktransfer_customers') IS NOT NULL THEN
    EXECUTE 'COMMENT ON TABLE public.stocktransfer_customers IS ''报关/客户侧移库单（原 stocktransfer）''';
  END IF;
  IF to_regclass('public.stocktransfer_item_customers') IS NOT NULL THEN
    EXECUTE 'COMMENT ON TABLE public.stocktransfer_item_customers IS ''报关移库明细（原 stocktransferitem）''';
  END IF;
END
$cmt$;

COMMENT ON TABLE public.stocktransfer_manual IS '手工移库主表';
COMMENT ON TABLE public.stocktransfer_item_manual IS '手工移库明细';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.stocktransfer_item_manual;

DROP TABLE IF EXISTS public.stocktransfer_manual;

DO $body$
BEGIN
  IF to_regclass('public.stocktransfer_item_customers') IS NOT NULL
     AND to_regclass('public.stocktransferitem') IS NULL THEN
    ALTER TABLE public.stocktransfer_item_customers RENAME TO stocktransferitem;
  END IF;

  IF to_regclass('public.stocktransfer_customers') IS NOT NULL
     AND to_regclass('public.stocktransfer') IS NULL THEN
    ALTER TABLE public.stocktransfer_customers RENAME TO stocktransfer;
  END IF;
END
$body$;
");
        }
    }
}
