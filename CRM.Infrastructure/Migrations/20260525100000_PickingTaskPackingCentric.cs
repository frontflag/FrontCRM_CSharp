using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>拣货任务以装箱单为主：StockOutRequestId 可空；packing_id 有效任务唯一。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260525100000_PickingTaskPackingCentric")]
public partial class PickingTaskPackingCentric : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.pickingtask
                ALTER COLUMN "StockOutRequestId" DROP NOT NULL;

            COMMENT ON COLUMN public.pickingtask."StockOutRequestId" IS '出库通知主键（按装箱单拣货时可为空）';

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_pickingtask_packing_id_active"
                ON public.pickingtask (packing_id)
                WHERE COALESCE(is_deleted, false) = false
                  AND packing_id IS NOT NULL
                  AND TRIM(packing_id) <> ''
                  AND "Status" <> -1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS "IX_pickingtask_packing_id_active";
            -- 不回填 NOT NULL，避免存在空值时失败
            """);
    }
}
