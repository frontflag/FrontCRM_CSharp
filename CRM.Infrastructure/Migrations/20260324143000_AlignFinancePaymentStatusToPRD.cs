using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324143000_AlignFinancePaymentStatusToPRD")]
    public partial class AlignFinancePaymentStatusToPRD : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'financepayment'
    ) THEN
        -- 旧状态映射到 PRD 新状态
        UPDATE public.financepayment SET ""Status"" = 1   WHERE ""Status"" = 0; -- 草稿 -> 新建
        UPDATE public.financepayment SET ""Status"" = 2   WHERE ""Status"" = 1; -- 待审核
        UPDATE public.financepayment SET ""Status"" = 10  WHERE ""Status"" = 2; -- 已审核 -> 审核通过
        UPDATE public.financepayment SET ""Status"" = 100 WHERE ""Status"" = 3; -- 已付款 -> 付款完成
        UPDATE public.financepayment SET ""Status"" = -2  WHERE ""Status"" IN (4, 5); -- 已取消/已作废 -> 取消

        ALTER TABLE public.financepayment
            ALTER COLUMN ""Status"" SET DEFAULT 1;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'financepayment'
    ) THEN
        -- 回滚映射（尽力）
        UPDATE public.financepayment SET ""Status"" = 0 WHERE ""Status"" = 1;
        UPDATE public.financepayment SET ""Status"" = 1 WHERE ""Status"" = 2;
        UPDATE public.financepayment SET ""Status"" = 2 WHERE ""Status"" = 10;
        UPDATE public.financepayment SET ""Status"" = 3 WHERE ""Status"" = 100;
        UPDATE public.financepayment SET ""Status"" = 4 WHERE ""Status"" = -2;
        UPDATE public.financepayment SET ""Status"" = 4 WHERE ""Status"" = -1;

        ALTER TABLE public.financepayment
            ALTER COLUMN ""Status"" SET DEFAULT 0;
    END IF;
END $$;
");
        }
    }
}
