using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260605140000_StockOutBatch")]
    public partial class AddStockOutBatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stock_out_batch (
    id character varying(36) NOT NULL,
    packing_id character varying(36) NOT NULL,
    global_batch_no character varying(20) NOT NULL,
    out_qty integer NOT NULL DEFAULT 0,
    is_deleted boolean NOT NULL DEFAULT false,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_stock_out_batch"" PRIMARY KEY (id),
    CONSTRAINT ""FK_stock_out_batch_packing"" FOREIGN KEY (packing_id)
        REFERENCES public.packing (""Id"") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ""UX_stock_out_batch_packing_global"" 
    ON public.stock_out_batch (packing_id, global_batch_no) WHERE is_deleted = false;
CREATE INDEX IF NOT EXISTS ""IX_stock_out_batch_global_batch_no"" ON public.stock_out_batch (global_batch_no);
CREATE INDEX IF NOT EXISTS ""IX_stock_out_batch_packing_id"" ON public.stock_out_batch (packing_id);

COMMENT ON TABLE public.stock_out_batch IS '出库批次：装箱单引用入库批次全局编号';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.stock_out_batch;");
        }
    }
}
