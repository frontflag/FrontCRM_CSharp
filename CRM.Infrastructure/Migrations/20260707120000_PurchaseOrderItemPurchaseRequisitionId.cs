using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// PO 明细增加 purchase_requisition_id：从 PR 生成 PO 时写入，修复同销售行多 PR 完成度误判。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260707120000_PurchaseOrderItemPurchaseRequisitionId")]
    public partial class PurchaseOrderItemPurchaseRequisitionId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.purchaseorderitem
  ADD COLUMN IF NOT EXISTS purchase_requisition_id character varying(36) NULL;

ALTER TABLE public.purchaseorderitem
  DROP CONSTRAINT IF EXISTS ""FK_purchaseorderitem_purchaserequisition_purchase_requisition_id"";

ALTER TABLE public.purchaseorderitem
  ADD CONSTRAINT ""FK_purchaseorderitem_purchaserequisition_purchase_requisition_id""
  FOREIGN KEY (purchase_requisition_id) REFERENCES public.purchaserequisition (""PurchaseRequisitionId"")
  ON DELETE RESTRICT;

CREATE INDEX IF NOT EXISTS ix_purchaseorderitem_purchase_requisition_id
  ON public.purchaseorderitem (purchase_requisition_id)
  WHERE purchase_requisition_id IS NOT NULL;

COMMENT ON COLUMN public.purchaseorderitem.purchase_requisition_id IS
  '来源采购申请 ID；从 PR 生成 PO 时写入，用于 PR 完成度与下游展示';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.ix_purchaseorderitem_purchase_requisition_id;

ALTER TABLE public.purchaseorderitem
  DROP CONSTRAINT IF EXISTS ""FK_purchaseorderitem_purchaserequisition_purchase_requisition_id"";

ALTER TABLE public.purchaseorderitem
  DROP COLUMN IF EXISTS purchase_requisition_id;
");
        }
    }
}
