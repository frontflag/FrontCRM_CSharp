using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324223000_AddInventoryCenterTables")]
    public partial class AddInventoryCenterTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.warehouseinfo (
    ""Id"" character varying(36) NOT NULL,
    ""WarehouseCode"" character varying(32) NOT NULL,
    ""WarehouseName"" character varying(100) NOT NULL,
    ""Address"" character varying(200),
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_warehouseinfo"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_warehouseinfo_WarehouseCode"" ON public.warehouseinfo (""WarehouseCode"");

CREATE TABLE IF NOT EXISTS public.warehousezone (
    ""Id"" character varying(36) NOT NULL,
    ""WarehouseId"" character varying(36) NOT NULL,
    ""ZoneCode"" character varying(32) NOT NULL,
    ""ZoneName"" character varying(100) NOT NULL,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_warehousezone"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_warehousezone_WarehouseId_ZoneCode"" ON public.warehousezone (""WarehouseId"", ""ZoneCode"");

CREATE TABLE IF NOT EXISTS public.warehouselocation (
    ""Id"" character varying(36) NOT NULL,
    ""ZoneId"" character varying(36) NOT NULL,
    ""LocationCode"" character varying(32) NOT NULL,
    ""LocationName"" character varying(100) NOT NULL,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_warehouselocation"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_warehouselocation_ZoneId_LocationCode"" ON public.warehouselocation (""ZoneId"", ""LocationCode"");

CREATE TABLE IF NOT EXISTS public.warehouseshelf (
    ""Id"" character varying(36) NOT NULL,
    ""LocationId"" character varying(36) NOT NULL,
    ""ShelfCode"" character varying(32) NOT NULL,
    ""ShelfName"" character varying(100) NOT NULL,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_warehouseshelf"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_warehouseshelf_LocationId_ShelfCode"" ON public.warehouseshelf (""LocationId"", ""ShelfCode"");

CREATE TABLE IF NOT EXISTS public.inventoryledger (
    ""Id"" character varying(36) NOT NULL,
    ""BizType"" character varying(20) NOT NULL,
    ""BizId"" character varying(36) NOT NULL,
    ""BizLineId"" character varying(36),
    ""MaterialId"" character varying(36) NOT NULL,
    ""WarehouseId"" character varying(36) NOT NULL,
    ""LocationId"" character varying(36),
    ""BatchNo"" character varying(50),
    ""QtyIn"" numeric(18,4) NOT NULL DEFAULT 0,
    ""QtyOut"" numeric(18,4) NOT NULL DEFAULT 0,
    ""UnitCost"" numeric(18,6) NOT NULL DEFAULT 0,
    ""Amount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""Remark"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_inventoryledger"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_inventoryledger_BizKey"" ON public.inventoryledger (""BizType"", ""BizId"", ""BizLineId"");

CREATE TABLE IF NOT EXISTS public.pickingtask (
    ""Id"" character varying(36) NOT NULL,
    ""TaskCode"" character varying(32) NOT NULL,
    ""StockOutRequestId"" character varying(36) NOT NULL,
    ""WarehouseId"" character varying(36) NOT NULL,
    ""OperatorId"" character varying(36) NOT NULL,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""Remark"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_pickingtask"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_pickingtask_TaskCode"" ON public.pickingtask (""TaskCode"");

CREATE TABLE IF NOT EXISTS public.pickingtaskitem (
    ""Id"" character varying(36) NOT NULL,
    ""PickingTaskId"" character varying(36) NOT NULL,
    ""MaterialId"" character varying(36) NOT NULL,
    ""StockId"" character varying(36),
    ""BatchNo"" character varying(50),
    ""LocationId"" character varying(36),
    ""PlanQty"" numeric(18,4) NOT NULL DEFAULT 0,
    ""PickedQty"" numeric(18,4) NOT NULL DEFAULT 0,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_pickingtaskitem"" PRIMARY KEY (""Id"")
);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_pickingtaskitem_pickingtask_PickingTaskId') THEN
        ALTER TABLE public.pickingtaskitem
            ADD CONSTRAINT ""FK_pickingtaskitem_pickingtask_PickingTaskId""
            FOREIGN KEY (""PickingTaskId"") REFERENCES public.pickingtask (""Id"") ON DELETE CASCADE;
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS public.inventorycountplan (
    ""Id"" character varying(36) NOT NULL,
    ""PlanMonth"" character varying(7) NOT NULL,
    ""WarehouseId"" character varying(36) NOT NULL,
    ""CreatorId"" character varying(36) NOT NULL,
    ""SubmitterId"" character varying(36),
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""Remark"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_inventorycountplan"" PRIMARY KEY (""Id"")
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_inventorycountplan_PlanMonth_WarehouseId"" ON public.inventorycountplan (""PlanMonth"", ""WarehouseId"");

CREATE TABLE IF NOT EXISTS public.inventorycountitem (
    ""Id"" character varying(36) NOT NULL,
    ""PlanId"" character varying(36) NOT NULL,
    ""MaterialId"" character varying(36) NOT NULL,
    ""LocationId"" character varying(36),
    ""BookQty"" numeric(18,4) NOT NULL DEFAULT 0,
    ""CountQty"" numeric(18,4) NOT NULL DEFAULT 0,
    ""DiffQty"" numeric(18,4) NOT NULL DEFAULT 0,
    ""BookAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""CountAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""DiffAmount"" numeric(18,2) NOT NULL DEFAULT 0,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_inventorycountitem"" PRIMARY KEY (""Id"")
);
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_inventorycountitem_inventorycountplan_PlanId') THEN
        ALTER TABLE public.inventorycountitem
            ADD CONSTRAINT ""FK_inventorycountitem_inventorycountplan_PlanId""
            FOREIGN KEY (""PlanId"") REFERENCES public.inventorycountplan (""Id"") ON DELETE CASCADE;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 为避免误删业务数据，此迁移不自动删除表
        }
    }
}

