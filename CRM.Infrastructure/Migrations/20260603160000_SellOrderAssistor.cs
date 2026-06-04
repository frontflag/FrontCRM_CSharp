using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>销售订单主表增加销售助理 <c>assistor</c>（用户 GUID）。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260603160000_SellOrderAssistor")]
public partial class SellOrderAssistor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'sellorder' AND column_name = 'assistor'
  ) THEN
    ALTER TABLE public.sellorder ADD COLUMN assistor character varying(36) NULL;
  END IF;
END $$;

COMMENT ON COLUMN public.sellorder.assistor IS '销售助理用户ID（sys_user.UserId），商务部跟进本销售订单';

CREATE INDEX IF NOT EXISTS ""IX_sellorder_assistor""
  ON public.sellorder (assistor)
  WHERE assistor IS NOT NULL;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_sellorder_assistor"";
ALTER TABLE IF EXISTS public.sellorder DROP COLUMN IF EXISTS assistor;
");
    }
}
