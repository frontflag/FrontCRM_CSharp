using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>装箱单主从表：packing / packing_extend_box / packing_item / packing_item_extend。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518100000_AddPackingTables")]
    public partial class AddPackingTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.packing (
    ""Id"" character varying(36) NOT NULL,
    ""Code"" character varying(32) NOT NULL,
    ""Status"" smallint NOT NULL DEFAULT 10,
    ""StockOutType"" smallint NOT NULL DEFAULT 10,
    ""MaterialType"" smallint NOT NULL DEFAULT 10,
    customer_id character varying(36) NULL,
    sales_id character varying(36) NULL,
    schedule_ship_date timestamp with time zone NULL,
    storage_id character varying(36) NULL,
    item_rows integer NOT NULL DEFAULT 0,
    comment character varying(500) NULL,
    create_by_user_id character varying(36) NULL,
    modify_by_user_id character varying(36) NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_packing"" PRIMARY KEY (""Id"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_packing_Code"" ON public.packing (""Code"") WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ""IX_packing_customer_id"" ON public.packing (customer_id) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ""IX_packing_Status"" ON public.packing (""Status"") WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS public.packing_extend_box (
    ""Id"" character varying(36) NOT NULL,
    ""PackingId"" character varying(36) NOT NULL,
    ""NW"" numeric(18,4) NULL,
    ""GW"" numeric(18,4) NULL,
    ""DIM"" character varying(200) NULL,
    ""CTNS"" integer NULL,
    CONSTRAINT ""PK_packing_extend_box"" PRIMARY KEY (""Id"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_packing_extend_box_PackingId"" ON public.packing_extend_box (""PackingId"");

CREATE TABLE IF NOT EXISTS public.packing_item (
    ""Id"" character varying(36) NOT NULL,
    ""PackingId"" character varying(36) NOT NULL,
    sell_order_id character varying(36) NULL,
    sell_order_item_id character varying(36) NULL,
    product_id character varying(36) NULL,
    stock_item_id character varying(36) NULL,
    ""PN"" character varying(200) NULL,
    ""Brand"" character varying(200) NULL,
    ""Qty"" integer NOT NULL DEFAULT 0,
    ""Unit"" character varying(20) NULL,
    ""CO"" character varying(64) NULL,
    comment character varying(500) NULL,
    create_by_user_id character varying(36) NULL,
    modify_by_user_id character varying(36) NULL,
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_packing_item"" PRIMARY KEY (""Id"")
);

CREATE INDEX IF NOT EXISTS ""IX_packing_item_PackingId"" ON public.packing_item (""PackingId"") WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ""IX_packing_item_stock_item_id"" ON public.packing_item (stock_item_id) WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS public.packing_item_extend (
    ""Id"" character varying(36) NOT NULL,
    ""PackingItemId"" character varying(36) NOT NULL,
    customer_id character varying(36) NULL,
    sales_id character varying(36) NULL,
    sell_order_id character varying(36) NULL,
    sell_order_item_id character varying(36) NULL,
    ""Price"" numeric(18,6) NULL,
    ""PriceCurrency"" smallint NULL,
    ""PriceConvertPrice"" numeric(18,6) NULL,
    customer_so character varying(200) NULL,
    customer_pn character varying(200) NULL,
    customer_brand character varying(200) NULL,
    CONSTRAINT ""PK_packing_item_extend"" PRIMARY KEY (""Id"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_packing_item_extend_PackingItemId"" ON public.packing_item_extend (""PackingItemId"");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_extend_box_packing_PackingId') THEN
        ALTER TABLE public.packing_extend_box
            ADD CONSTRAINT ""FK_packing_extend_box_packing_PackingId""
            FOREIGN KEY (""PackingId"") REFERENCES public.packing (""Id"") ON DELETE CASCADE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_item_packing_PackingId') THEN
        ALTER TABLE public.packing_item
            ADD CONSTRAINT ""FK_packing_item_packing_PackingId""
            FOREIGN KEY (""PackingId"") REFERENCES public.packing (""Id"") ON DELETE CASCADE;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_item_extend_packing_item_PackingItemId') THEN
        ALTER TABLE public.packing_item_extend
            ADD CONSTRAINT ""FK_packing_item_extend_packing_item_PackingItemId""
            FOREIGN KEY (""PackingItemId"") REFERENCES public.packing_item (""Id"") ON DELETE CASCADE;
    END IF;
END $$;

INSERT INTO sys_serial_number (""Id"", ""CreateTime"", ""CurrentSequence"", ""ModuleCode"", ""ModuleName"", ""Prefix"", ""SequenceLength"", ""ResetByMonth"", ""ResetByYear"")
SELECT (SELECT COALESCE(MAX(""Id""), 0) + 1 FROM sys_serial_number),
       TIMESTAMPTZ '2026-01-01T00:00:00Z', 2025, 'Packing', '装箱单', 'Pak', 5, false, false
WHERE NOT EXISTS (SELECT 1 FROM sys_serial_number WHERE ""ModuleCode"" = 'Packing');

COMMENT ON TABLE public.packing IS '装箱单主表';
COMMENT ON COLUMN public.packing.""Code"" IS '装箱单编号，流水前缀 Pak';
COMMENT ON COLUMN public.packing.""Status"" IS '10新建 20已确认 30已拣货 40已备货 50待出库 100出库完成';
COMMENT ON COLUMN public.packing.""StockOutType"" IS '10销售出库 20报关出库 30退货出库 40报废出库';
COMMENT ON COLUMN public.packing.""MaterialType"" IS '10常物料 20测试物料 30样品物料';
COMMENT ON COLUMN public.packing.storage_id IS '库存汇总层主键 stock.StockId';
COMMENT ON COLUMN public.packing.item_rows IS '分拣单明细行数';

COMMENT ON TABLE public.packing_extend_box IS '装箱单箱规扩展：净重/毛重/尺寸/箱数';
COMMENT ON TABLE public.packing_item IS '装箱单明细';
COMMENT ON TABLE public.packing_item_extend IS '装箱单明细扩展：客户/业务员/销售价快照';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM sys_serial_number WHERE ""ModuleCode"" = 'Packing';
DROP TABLE IF EXISTS public.packing_item_extend;
DROP TABLE IF EXISTS public.packing_item;
DROP TABLE IF EXISTS public.packing_extend_box;
DROP TABLE IF EXISTS public.packing;
");
        }
    }
}
