using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 库存/质检/入库/出库/拣货/报关 等业务表增加 <c>is_deleted</c>（与 <see cref="CRM.Core.Models.Purchase.PurchaseRequisition.IsDeleted"/> 列名一致），
/// 并调整部分唯一索引为「仅未删除行」唯一，避免软删后占号。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260708120000_InventoryLogisticsCustomsSoftDelete")]
public partial class InventoryLogisticsCustomsSoftDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.qcinfo ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.qcitem ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.stock_in ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_in_item ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_in_item_extend ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_in_extend ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_in_batch ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.stock ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_extend ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_item ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.stock_out ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_out_item ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stock_out_item_extend ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.pickingtask ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.pickingtaskitem ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

ALTER TABLE IF EXISTS public.customs_declaration ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.customs_declaration_item ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stocktransfer_customers ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS public.stocktransfer_item_customers ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;

-- stock：业务编号唯一（排除已删）
DROP INDEX IF EXISTS public.""IX_stock_StockCode_unique_not_null"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stock_StockCode_unique_not_null""
  ON public.stock (""StockCode"")
  WHERE ""StockCode"" IS NOT NULL AND is_deleted = false;

-- 入库明细行号唯一（排除已删）
DROP INDEX IF EXISTS public.""IX_stockinitem_stockin_linecode"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stockinitem_stockin_linecode""
  ON public.stock_in_item (""StockInId"", stock_in_item_code)
  WHERE is_deleted = false AND stock_in_item_code IS NOT NULL;

-- 在库明细
DROP INDEX IF EXISTS public.""IX_stockitem_StockInItemId"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stockitem_StockInItemId""
  ON public.stock_item (""StockInItemId"")
  WHERE is_deleted = false;

DROP INDEX IF EXISTS public.""IX_stock_item_agg_linecode"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stock_item_agg_linecode""
  ON public.stock_item (""StockAggregateId"", stock_item_code)
  WHERE is_deleted = false;

-- 拣货任务单号
DROP INDEX IF EXISTS public.""IX_pickingtask_TaskCode"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_pickingtask_TaskCode""
  ON public.pickingtask (""TaskCode"")
  WHERE is_deleted = false;

-- 报关主单（若曾存在 PascalCase 列名迁移，统一为 is_deleted）
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""IsDeleted"";
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""DeletedAt"";
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS ""deleted_by_user_id"";

DROP INDEX IF EXISTS public.""IX_customs_declaration_DeclarationCode"";
DROP INDEX IF EXISTS public.""IX_customs_declaration_StockOutRequestId"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_DeclarationCode""
  ON public.customs_declaration (""DeclarationCode"")
  WHERE is_deleted = false;
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_StockOutRequestId""
  ON public.customs_declaration (""StockOutRequestId"")
  WHERE is_deleted = false;

-- 报关移库（表由 stocktransfer 重命名而来，索引名可能仍为 IX_stocktransfer_*）
DROP INDEX IF EXISTS public.""IX_stocktransfer_TransferCode"";
DROP INDEX IF EXISTS public.""IX_stocktransfer_CustomsDeclarationId"";
DROP INDEX IF EXISTS public.""IX_stocktransfer_customers_TransferCode"";
DROP INDEX IF EXISTS public.""IX_stocktransfer_customers_CustomsDeclarationId"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stocktransfer_customers_TransferCode""
  ON public.stocktransfer_customers (""TransferCode"")
  WHERE is_deleted = false;
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stocktransfer_customers_CustomsDeclarationId""
  ON public.stocktransfer_customers (""CustomsDeclarationId"")
  WHERE is_deleted = false;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_stocktransfer_customers_CustomsDeclarationId"";
DROP INDEX IF EXISTS public.""IX_stocktransfer_customers_TransferCode"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stocktransfer_TransferCode""
  ON public.stocktransfer_customers (""TransferCode"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stocktransfer_CustomsDeclarationId""
  ON public.stocktransfer_customers (""CustomsDeclarationId"");

DROP INDEX IF EXISTS public.""IX_customs_declaration_StockOutRequestId"";
DROP INDEX IF EXISTS public.""IX_customs_declaration_DeclarationCode"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_DeclarationCode""
  ON public.customs_declaration (""DeclarationCode"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_customs_declaration_StockOutRequestId""
  ON public.customs_declaration (""StockOutRequestId"");

DROP INDEX IF EXISTS public.""IX_pickingtask_TaskCode"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_pickingtask_TaskCode""
  ON public.pickingtask (""TaskCode"");

DROP INDEX IF EXISTS public.""IX_stock_item_agg_linecode"";
DROP INDEX IF EXISTS public.""IX_stockitem_StockInItemId"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stockitem_StockInItemId""
  ON public.stock_item (""StockInItemId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stock_item_agg_linecode""
  ON public.stock_item (""StockAggregateId"", stock_item_code);

DROP INDEX IF EXISTS public.""IX_stockinitem_stockin_linecode"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stockinitem_stockin_linecode""
  ON public.stock_in_item (""StockInId"", stock_in_item_code);

DROP INDEX IF EXISTS public.""IX_stock_StockCode_unique_not_null"";
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_stock_StockCode_unique_not_null""
  ON public.stock (""StockCode"")
  WHERE ""StockCode"" IS NOT NULL;

ALTER TABLE IF EXISTS public.stocktransfer_item_customers DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stocktransfer_customers DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.customs_declaration_item DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.customs_declaration DROP COLUMN IF EXISTS is_deleted;

ALTER TABLE IF EXISTS public.pickingtaskitem DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.pickingtask DROP COLUMN IF EXISTS is_deleted;

ALTER TABLE IF EXISTS public.stock_out_item_extend DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock_out_item DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock_out DROP COLUMN IF EXISTS is_deleted;

ALTER TABLE IF EXISTS public.stock_item DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock_extend DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock DROP COLUMN IF EXISTS is_deleted;

ALTER TABLE IF EXISTS public.stock_in_batch DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock_in_extend DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock_in_item_extend DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock_in_item DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.stock_in DROP COLUMN IF EXISTS is_deleted;

ALTER TABLE IF EXISTS public.qcitem DROP COLUMN IF EXISTS is_deleted;
ALTER TABLE IF EXISTS public.qcinfo DROP COLUMN IF EXISTS is_deleted;
");
    }
}
