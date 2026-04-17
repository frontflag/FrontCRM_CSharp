using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 撤回移库明细上的报关费用列：费用仅保留在 <c>customs_declaration_item</c>，通过 <c>CustomsDeclarationItemId</c> 关联查询。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260614100000_StockTransferItemRemoveFeeSnapshotColumns")]
    public partial class StockTransferItemRemoveFeeSnapshotColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""TaxIncludedUnitPrice"";
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""TotalValueTax"";
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""InspectionFee"";
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""OtherFee"";
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""CustomsAgencyFee"";
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""VatAmount"";
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""DutyAmount"";
ALTER TABLE IF EXISTS public.stocktransferitem DROP COLUMN IF EXISTS ""CustomsPaymentGoods"";
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""CustomsPaymentGoods"" numeric(18,2) NULL;
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""DutyAmount"" numeric(18,2) NULL;
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""VatAmount"" numeric(18,2) NULL;
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""CustomsAgencyFee"" numeric(18,2) NULL;
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""OtherFee"" numeric(18,2) NULL;
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""InspectionFee"" numeric(18,2) NULL;
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""TotalValueTax"" numeric(18,2) NULL;
ALTER TABLE IF EXISTS public.stocktransferitem
  ADD COLUMN IF NOT EXISTS ""TaxIncludedUnitPrice"" numeric(18,6) NULL;
");
        }
    }
}
