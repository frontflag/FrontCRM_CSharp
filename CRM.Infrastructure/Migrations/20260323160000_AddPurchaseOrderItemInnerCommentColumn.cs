using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 确保 purchaseorderitem 表存在 inner_comment 列（如果已存在则跳过）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260323160000_AddPurchaseOrderItemInnerCommentColumn")]
    public partial class AddPurchaseOrderItemInnerCommentColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Postgres 支持 IF NOT EXISTS，但列级别在不同版本/场景下兼容性不一，这里用 information_schema 做判断。
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'purchaseorderitem'
          AND column_name = 'inner_comment'
    ) THEN
        ALTER TABLE public.purchaseorderitem
        ADD COLUMN inner_comment varchar(500);
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.purchaseorderitem DROP COLUMN IF EXISTS inner_comment;");
        }
    }
}

