using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260327120000_AddStockInNotifyExpectedArrivalDate")]
    public partial class AddStockInNotifyExpectedArrivalDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.stockinnotify ADD COLUMN IF NOT EXISTS ""ExpectedArrivalDate"" timestamp with time zone NULL;
COMMENT ON COLUMN public.stockinnotify.""ExpectedArrivalDate"" IS '预计到货日期（通知物流关注）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE public.stockinnotify DROP COLUMN IF EXISTS ""ExpectedArrivalDate"";");
        }
    }
}
