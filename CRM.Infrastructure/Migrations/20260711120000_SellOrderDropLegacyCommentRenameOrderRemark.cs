using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 删除主表历史拼接列 <c>comment</c>，将 <c>order_remark</c> 重命名为 <c>comment</c>（物理列语义：自由备注）。
    /// 执行前请先跑 Debug「拆分」或确保旧 <c>comment</c> 中需保留的信息已迁移到结构化列/<c>order_remark</c>，否则 DROP 会丢失旧列数据。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260711120000_SellOrderDropLegacyCommentRenameOrderRemark")]
    public partial class SellOrderDropLegacyCommentRenameOrderRemark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS comment;
                ALTER TABLE IF EXISTS public.sellorder RENAME COLUMN order_remark TO comment;
                COMMENT ON COLUMN public.sellorder.comment IS '订单备注（自由文本；原 order_remark）';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE IF EXISTS public.sellorder RENAME COLUMN comment TO order_remark;
                ALTER TABLE IF EXISTS public.sellorder ADD COLUMN IF NOT EXISTS comment character varying(500) NULL;
                COMMENT ON COLUMN public.sellorder.order_remark IS '订单自由备注（不含结构化前缀行）';
                """);
        }
    }
}
