using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 库存域数量列由 numeric(18,4) 改为 integer（四舍五入取整，与阶段 0 脚本一致）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260511120000_InventoryQuantityColumnsToInteger")]
    public partial class InventoryQuantityColumnsToInteger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock
  ALTER COLUMN ""Qty"" TYPE integer USING (round(""Qty"")::integer),
  ALTER COLUMN ""QtyStockOut"" TYPE integer USING (round(""QtyStockOut"")::integer),
  ALTER COLUMN ""QtyOccupy"" TYPE integer USING (round(""QtyOccupy"")::integer),
  ALTER COLUMN ""QtySales"" TYPE integer USING (round(""QtySales"")::integer),
  ALTER COLUMN ""QtyRepertory"" TYPE integer USING (round(""QtyRepertory"")::integer),
  ALTER COLUMN ""QtyRepertoryAvailable"" TYPE integer USING (round(""QtyRepertoryAvailable"")::integer);

ALTER TABLE IF EXISTS public.stockin
  ALTER COLUMN ""TotalQuantity"" TYPE integer USING (round(""TotalQuantity"")::integer);

ALTER TABLE IF EXISTS public.stockinitem
  ALTER COLUMN ""Quantity"" TYPE integer USING (round(""Quantity"")::integer),
  ALTER COLUMN ""OrderQty"" TYPE integer USING (round(""OrderQty"")::integer),
  ALTER COLUMN ""QtyReceived"" TYPE integer USING (round(""QtyReceived"")::integer);

ALTER TABLE IF EXISTS public.stockout
  ALTER COLUMN ""TotalQuantity"" TYPE integer USING (round(""TotalQuantity"")::integer);

ALTER TABLE IF EXISTS public.stockoutitem
  ALTER COLUMN ""Quantity"" TYPE integer USING (round(""Quantity"")::integer),
  ALTER COLUMN ""OrderQty"" TYPE integer USING (round(""OrderQty"")::integer),
  ALTER COLUMN ""PlanQty"" TYPE integer USING (round(""PlanQty"")::integer),
  ALTER COLUMN ""PickQty"" TYPE integer USING (round(""PickQty"")::integer),
  ALTER COLUMN ""ActualQty"" TYPE integer USING (round(""ActualQty"")::integer);

ALTER TABLE IF EXISTS public.stockledger
  ALTER COLUMN ""QtyIn"" TYPE integer USING (round(""QtyIn"")::integer),
  ALTER COLUMN ""QtyOut"" TYPE integer USING (round(""QtyOut"")::integer);

ALTER TABLE IF EXISTS public.stockoutrequest
  ALTER COLUMN ""Quantity"" TYPE integer USING (round(""Quantity"")::integer);

ALTER TABLE IF EXISTS public.pickingtaskitem
  ALTER COLUMN ""PlanQty"" TYPE integer USING (round(""PlanQty"")::integer),
  ALTER COLUMN ""PickedQty"" TYPE integer USING (round(""PickedQty"")::integer);

ALTER TABLE IF EXISTS public.stockinnotify
  ALTER COLUMN ""ExpectQty"" TYPE integer USING (round(""ExpectQty"")::integer),
  ALTER COLUMN ""ReceiveQty"" TYPE integer USING (round(""ReceiveQty"")::integer),
  ALTER COLUMN ""PassedQty"" TYPE integer USING (round(""PassedQty"")::integer);

ALTER TABLE IF EXISTS public.qcinfo
  ALTER COLUMN ""PassQty"" TYPE integer USING (round(""PassQty"")::integer),
  ALTER COLUMN ""RejectQty"" TYPE integer USING (round(""RejectQty"")::integer);

ALTER TABLE IF EXISTS public.qcitem
  ALTER COLUMN ""ArrivedQty"" TYPE integer USING (round(""ArrivedQty"")::integer),
  ALTER COLUMN ""PassedQty"" TYPE integer USING (round(""PassedQty"")::integer),
  ALTER COLUMN ""RejectQty"" TYPE integer USING (round(""RejectQty"")::integer);

ALTER TABLE IF EXISTS public.inventorycountitem
  ALTER COLUMN ""BookQty"" TYPE integer USING (round(""BookQty"")::integer),
  ALTER COLUMN ""CountQty"" TYPE integer USING (round(""CountQty"")::integer),
  ALTER COLUMN ""DiffQty"" TYPE integer USING (round(""DiffQty"")::integer);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stock
  ALTER COLUMN ""Qty"" TYPE numeric(18,4) USING (""Qty""::numeric),
  ALTER COLUMN ""QtyStockOut"" TYPE numeric(18,4) USING (""QtyStockOut""::numeric),
  ALTER COLUMN ""QtyOccupy"" TYPE numeric(18,4) USING (""QtyOccupy""::numeric),
  ALTER COLUMN ""QtySales"" TYPE numeric(18,4) USING (""QtySales""::numeric),
  ALTER COLUMN ""QtyRepertory"" TYPE numeric(18,4) USING (""QtyRepertory""::numeric),
  ALTER COLUMN ""QtyRepertoryAvailable"" TYPE numeric(18,4) USING (""QtyRepertoryAvailable""::numeric);

ALTER TABLE IF EXISTS public.stockin
  ALTER COLUMN ""TotalQuantity"" TYPE numeric(18,4) USING (""TotalQuantity""::numeric);

ALTER TABLE IF EXISTS public.stockinitem
  ALTER COLUMN ""Quantity"" TYPE numeric(18,4) USING (""Quantity""::numeric),
  ALTER COLUMN ""OrderQty"" TYPE numeric(18,4) USING (""OrderQty""::numeric),
  ALTER COLUMN ""QtyReceived"" TYPE numeric(18,4) USING (""QtyReceived""::numeric);

ALTER TABLE IF EXISTS public.stockout
  ALTER COLUMN ""TotalQuantity"" TYPE numeric(18,4) USING (""TotalQuantity""::numeric);

ALTER TABLE IF EXISTS public.stockoutitem
  ALTER COLUMN ""Quantity"" TYPE numeric(18,4) USING (""Quantity""::numeric),
  ALTER COLUMN ""OrderQty"" TYPE numeric(18,4) USING (""OrderQty""::numeric),
  ALTER COLUMN ""PlanQty"" TYPE numeric(18,4) USING (""PlanQty""::numeric),
  ALTER COLUMN ""PickQty"" TYPE numeric(18,4) USING (""PickQty""::numeric),
  ALTER COLUMN ""ActualQty"" TYPE numeric(18,4) USING (""ActualQty""::numeric);

ALTER TABLE IF EXISTS public.stockledger
  ALTER COLUMN ""QtyIn"" TYPE numeric(18,4) USING (""QtyIn""::numeric),
  ALTER COLUMN ""QtyOut"" TYPE numeric(18,4) USING (""QtyOut""::numeric);

ALTER TABLE IF EXISTS public.stockoutrequest
  ALTER COLUMN ""Quantity"" TYPE numeric(18,4) USING (""Quantity""::numeric);

ALTER TABLE IF EXISTS public.pickingtaskitem
  ALTER COLUMN ""PlanQty"" TYPE numeric(18,4) USING (""PlanQty""::numeric),
  ALTER COLUMN ""PickedQty"" TYPE numeric(18,4) USING (""PickedQty""::numeric);

ALTER TABLE IF EXISTS public.stockinnotify
  ALTER COLUMN ""ExpectQty"" TYPE numeric(18,4) USING (""ExpectQty""::numeric),
  ALTER COLUMN ""ReceiveQty"" TYPE numeric(18,4) USING (""ReceiveQty""::numeric),
  ALTER COLUMN ""PassedQty"" TYPE numeric(18,4) USING (""PassedQty""::numeric);

ALTER TABLE IF EXISTS public.qcinfo
  ALTER COLUMN ""PassQty"" TYPE numeric(18,4) USING (""PassQty""::numeric),
  ALTER COLUMN ""RejectQty"" TYPE numeric(18,4) USING (""RejectQty""::numeric);

ALTER TABLE IF EXISTS public.qcitem
  ALTER COLUMN ""ArrivedQty"" TYPE numeric(18,4) USING (""ArrivedQty""::numeric),
  ALTER COLUMN ""PassedQty"" TYPE numeric(18,4) USING (""PassedQty""::numeric),
  ALTER COLUMN ""RejectQty"" TYPE numeric(18,4) USING (""RejectQty""::numeric);

ALTER TABLE IF EXISTS public.inventorycountitem
  ALTER COLUMN ""BookQty"" TYPE numeric(18,4) USING (""BookQty""::numeric),
  ALTER COLUMN ""CountQty"" TYPE numeric(18,4) USING (""CountQty""::numeric),
  ALTER COLUMN ""DiffQty"" TYPE numeric(18,4) USING (""DiffQty""::numeric);
");
        }
    }
}
