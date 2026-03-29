using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 全系统「最近打开详情/编辑」访问记录；按用户 + 业务类型保留最近 100 条不同实体（同实体重复打开会刷新时间）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260328230000_AddLogRecentTable")]
    public partial class AddLogRecentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.log_recent (
    ""Id"" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    ""BizType"" VARCHAR(64) NOT NULL,
    ""RecordId"" TEXT NOT NULL,
    ""RecordCode"" VARCHAR(128),
    ""AccessedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ""UserId"" TEXT NOT NULL DEFAULT '',
    ""OpenKind"" VARCHAR(16) NOT NULL DEFAULT 'detail'
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_log_recent_user_biz_record ON public.log_recent(""UserId"", ""BizType"", ""RecordId"");
CREATE INDEX IF NOT EXISTS idx_log_recent_user_biz_time ON public.log_recent(""UserId"", ""BizType"", ""AccessedAt"" DESC);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.idx_log_recent_user_biz_time;
DROP INDEX IF EXISTS public.ux_log_recent_user_biz_record;
DROP TABLE IF EXISTS public.log_recent;
");
        }
    }
}
