using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>拣货任务仅关联装箱单，移除 pickingtask.StockOutRequestId。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260526100000_DropPickingTaskStockOutRequestId")]
public partial class DropPickingTaskStockOutRequestId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.pickingtask pt
            SET packing_id = sub.packing_id
            FROM (
                SELECT DISTINCT ON (TRIM(pi.stockout_notify_id))
                    TRIM(pi.stockout_notify_id) AS stock_out_request_id,
                    pi."PackingId" AS packing_id
                FROM public.packing_item pi
                WHERE COALESCE(pi.is_deleted, false) = false
                  AND pi.stockout_notify_id IS NOT NULL
                  AND TRIM(pi.stockout_notify_id) <> ''
                ORDER BY TRIM(pi.stockout_notify_id), pi."CreateTime" DESC NULLS LAST, pi."Id"
            ) sub
            WHERE COALESCE(pt.is_deleted, false) = false
              AND pt."StockOutRequestId" IS NOT NULL
              AND TRIM(pt."StockOutRequestId") = sub.stock_out_request_id
              AND (pt.packing_id IS NULL OR TRIM(pt.packing_id) = '');

            ALTER TABLE IF EXISTS public.pickingtask
                DROP COLUMN IF EXISTS "StockOutRequestId";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE IF EXISTS public.pickingtask
                ADD COLUMN IF NOT EXISTS "StockOutRequestId" character varying(36) NULL;

            UPDATE public.pickingtask pt
            SET "StockOutRequestId" = sub.stock_out_request_id
            FROM (
                SELECT DISTINCT ON (pi."PackingId")
                    pi."PackingId" AS packing_id,
                    TRIM(pi.stockout_notify_id) AS stock_out_request_id
                FROM public.packing_item pi
                WHERE COALESCE(pi.is_deleted, false) = false
                  AND pi.stockout_notify_id IS NOT NULL
                  AND TRIM(pi.stockout_notify_id) <> ''
                ORDER BY pi."PackingId", pi."CreateTime" ASC NULLS LAST, pi."Id"
            ) sub
            WHERE COALESCE(pt.is_deleted, false) = false
              AND pt.packing_id IS NOT NULL
              AND TRIM(pt.packing_id) = TRIM(sub.packing_id)
              AND pt."StockOutRequestId" IS NULL;
            """);
    }
}
