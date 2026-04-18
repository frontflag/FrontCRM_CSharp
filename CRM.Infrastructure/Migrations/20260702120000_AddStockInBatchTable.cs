using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260702120000_AddStockInBatchTable")]
    public partial class AddStockInBatchTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stock_in_batch (
    id character varying(36) NOT NULL,
    stock_in_id character varying(36) NOT NULL,
    stock_in_item_id character varying(36) NOT NULL,
    stock_in_item_code character varying(64) NULL,
    material_model character varying(200) NULL,
    dc character varying(64) NULL,
    package_origin character varying(200) NULL,
    wafer_origin character varying(200) NULL,
    lot character varying(128) NULL,
    lot_qty_in integer NOT NULL DEFAULT 0,
    lot_qty_out integer NOT NULL DEFAULT 0,
    origin character varying(200) NULL,
    serial_number character varying(200) NULL,
    sn_qty_in integer NOT NULL DEFAULT 0,
    sn_qty_out integer NOT NULL DEFAULT 0,
    firmware_version character varying(128) NULL,
    remark character varying(1000) NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_stock_in_batch"" PRIMARY KEY (id),
    CONSTRAINT ""FK_stock_in_batch_stock_in_item"" FOREIGN KEY (stock_in_item_id)
        REFERENCES public.stock_in_item(""ItemId"") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ""IX_stock_in_batch_stock_in_item_id"" ON public.stock_in_batch (stock_in_item_id);
CREATE INDEX IF NOT EXISTS ""IX_stock_in_batch_stock_in_id"" ON public.stock_in_batch (stock_in_id);
CREATE INDEX IF NOT EXISTS ""IX_stock_in_batch_stock_in_item_code"" ON public.stock_in_batch (stock_in_item_code);
CREATE INDEX IF NOT EXISTS ""IX_stock_in_batch_lot"" ON public.stock_in_batch (lot);
CREATE INDEX IF NOT EXISTS ""IX_stock_in_batch_serial_number"" ON public.stock_in_batch (serial_number);

COMMENT ON TABLE public.stock_in_batch IS '入库批次记录：LOT/SN/产地等';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.stock_in_batch;");
        }
    }
}
