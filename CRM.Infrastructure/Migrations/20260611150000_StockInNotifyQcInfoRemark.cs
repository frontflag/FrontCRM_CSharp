using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 到货通知与质检主表增加备注：采购创建到货通知写入 stockin_notify.Remark；质检保存写入 qcinfo.Remark。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260611150000_StockInNotifyQcInfoRemark")]
    public partial class StockInNotifyQcInfoRemark : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin_notify
  ADD COLUMN IF NOT EXISTS ""Remark"" character varying(500) NULL;
COMMENT ON COLUMN public.stockin_notify.""Remark"" IS '到货通知备注（采购创建到货通知时填写）';

ALTER TABLE IF EXISTS public.qcinfo
  ADD COLUMN IF NOT EXISTS ""Remark"" character varying(500) NULL;
COMMENT ON COLUMN public.qcinfo.""Remark"" IS '质检备注（质检保存时填写）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin_notify DROP COLUMN IF EXISTS ""Remark"";
ALTER TABLE IF EXISTS public.qcinfo DROP COLUMN IF EXISTS ""Remark"";
");
        }
    }
}
