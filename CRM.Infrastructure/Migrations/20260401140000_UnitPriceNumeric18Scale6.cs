using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 单价类字段统一 numeric(18,6)；总额类保持 numeric(18,2)（本次仅扩单价列精度）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260401140000_UnitPriceNumeric18Scale6")]
    public partial class UnitPriceNumeric18Scale6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $BODY$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockinitem') THEN
    ALTER TABLE public.stockinitem ALTER COLUMN ""Price"" TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutitem') THEN
    ALTER TABLE public.stockoutitem ALTER COLUMN ""Price"" TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'rfqitem') THEN
    ALTER TABLE public.rfqitem ALTER COLUMN target_price TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'financepurchaseinvoiceitem') THEN
    ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN ""StockInCost"" TYPE numeric(18,6);
    ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN ""BillCost"" TYPE numeric(18,6);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'sellinvoiceitem') THEN
    ALTER TABLE public.sellinvoiceitem ALTER COLUMN ""Price"" TYPE numeric(18,6);
  END IF;
END
$BODY$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $BODY$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockinitem') THEN
    ALTER TABLE public.stockinitem ALTER COLUMN ""Price"" TYPE numeric(18,4);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'stockoutitem') THEN
    ALTER TABLE public.stockoutitem ALTER COLUMN ""Price"" TYPE numeric(18,4);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'rfqitem') THEN
    ALTER TABLE public.rfqitem ALTER COLUMN target_price TYPE numeric(18,4);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'financepurchaseinvoiceitem') THEN
    ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN ""StockInCost"" TYPE numeric(18,4);
    ALTER TABLE public.financepurchaseinvoiceitem ALTER COLUMN ""BillCost"" TYPE numeric(18,4);
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'sellinvoiceitem') THEN
    ALTER TABLE public.sellinvoiceitem ALTER COLUMN ""Price"" TYPE numeric(18,4);
  END IF;
END
$BODY$;
");
        }
    }
}
