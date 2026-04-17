using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 曾向 <c>stocktransferitem</c> 增加费用快照列（已撤回：见 <c>20260614100000_StockTransferItemRemoveFeeSnapshotColumns</c>）；保留本迁移以满足已应用环境的 EF 历史记录。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260613100000_StockTransferItemFeeSnapshots")]
    public partial class StockTransferItemFeeSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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
COMMENT ON COLUMN public.stocktransferitem.""CustomsPaymentGoods"" IS '报关公司货款快照';
COMMENT ON COLUMN public.stocktransferitem.""DutyAmount"" IS '关税快照';
COMMENT ON COLUMN public.stocktransferitem.""VatAmount"" IS '增值税快照';
COMMENT ON COLUMN public.stocktransferitem.""CustomsAgencyFee"" IS '报关代理费快照';
COMMENT ON COLUMN public.stocktransferitem.""OtherFee"" IS '杂费快照';
COMMENT ON COLUMN public.stocktransferitem.""InspectionFee"" IS '商检费快照';
COMMENT ON COLUMN public.stocktransferitem.""TotalValueTax"" IS '价税合计（行）快照';
COMMENT ON COLUMN public.stocktransferitem.""TaxIncludedUnitPrice"" IS '含税单价（行）快照';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
