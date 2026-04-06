using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// stockin：冗余到货通知（SourceCode/SourceId）与质检单（QcCode/QCID），便于列表与追溯。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260414120000_AddStockInNotifyAndQcSourceColumns")]
    public partial class AddStockInNotifyAndQcSourceColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SourceCode"" character varying(32) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""SourceId"" character varying(36) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""QcCode"" character varying(32) NULL;
ALTER TABLE IF EXISTS public.stockin ADD COLUMN IF NOT EXISTS ""QCID"" character varying(36) NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SourceCode"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""SourceId"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""QcCode"";
ALTER TABLE IF EXISTS public.stockin DROP COLUMN IF EXISTS ""QCID"";
");
        }
    }
}
