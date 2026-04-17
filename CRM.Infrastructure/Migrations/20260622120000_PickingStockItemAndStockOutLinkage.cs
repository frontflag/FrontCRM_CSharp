using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 拣货明细绑定在库明细、出库单关联拣货任务、出库明细关联拣货明细行（与《库存拣货出库整改方案》一致）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260622120000_PickingStockItemAndStockOutLinkage")]
    public partial class PickingStockItemAndStockOutLinkage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockout ADD COLUMN IF NOT EXISTS ""picking_task_id"" character varying(36) NULL;
COMMENT ON COLUMN public.stockout.""picking_task_id"" IS '完成拣货后执行出库时关联的 pickingtask.Id';

ALTER TABLE IF EXISTS public.pickingtaskitem ADD COLUMN IF NOT EXISTS ""stock_item_id"" character varying(36) NULL;
COMMENT ON COLUMN public.pickingtaskitem.""stock_item_id"" IS '在库明细 stockitem.StockItemId；新流程拣货必填';

ALTER TABLE IF EXISTS public.stockoutitem ADD COLUMN IF NOT EXISTS ""picking_task_item_id"" character varying(36) NULL;
COMMENT ON COLUMN public.stockoutitem.""picking_task_item_id"" IS '来源拣货明细 pickingtaskitem.Id';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutitem DROP COLUMN IF EXISTS ""picking_task_item_id"";
ALTER TABLE IF EXISTS public.pickingtaskitem DROP COLUMN IF EXISTS ""stock_item_id"";
ALTER TABLE IF EXISTS public.stockout DROP COLUMN IF EXISTS ""picking_task_id"";
");
        }
    }
}
