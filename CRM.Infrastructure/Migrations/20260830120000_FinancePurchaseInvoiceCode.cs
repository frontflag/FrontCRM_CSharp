using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 进项发票主表增加系统单号 InvoiceCode（与销项 InvoiceCode 对齐），并为历史行回填。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260830120000_FinancePurchaseInvoiceCode")]
    public partial class FinancePurchaseInvoiceCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.financepurchaseinvoice
  ADD COLUMN IF NOT EXISTS ""InvoiceCode"" character varying(32) NULL;

COMMENT ON COLUMN public.financepurchaseinvoice.""InvoiceCode"" IS '发票单号（系统编号）';

-- 历史数据回填：按创建时间分配 INVI + 5 位 32 进制序号，并推进 InputInvoice 流水
DO $backfill$
DECLARE
  alphabet text := '0123456789ABCDEFGHKLMNPRSTUVWXYZ';
  r record;
  seq int := 0;
  prefix text := 'INVI';
  v int;
  rem int;
  encoded text;
  i int;
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'financepurchaseinvoice' AND column_name = 'InvoiceCode'
  ) THEN
    SELECT COALESCE(""CurrentSequence"", -1) INTO seq
    FROM public.sys_serial_number
    WHERE ""ModuleCode"" = 'InputInvoice'
    FOR UPDATE;

    IF seq IS NULL THEN
      seq := -1;
    END IF;

    FOR r IN
      SELECT ""FinancePurchaseInvoiceId""
      FROM public.financepurchaseinvoice
      WHERE ""InvoiceCode"" IS NULL OR btrim(""InvoiceCode"") = ''
      ORDER BY ""CreateTime"" NULLS LAST, ""FinancePurchaseInvoiceId""
    LOOP
      seq := seq + 1;
      v := seq;
      encoded := '';
      FOR i IN 1..5 LOOP
        rem := v % 32;
        encoded := substr(alphabet, rem + 1, 1) || encoded;
        v := v / 32;
      END LOOP;
      IF v > 0 THEN
        RAISE EXCEPTION 'InputInvoice 流水超出 5 位 32 进制范围';
      END IF;

      UPDATE public.financepurchaseinvoice
      SET ""InvoiceCode"" = prefix || encoded
      WHERE ""FinancePurchaseInvoiceId"" = r.""FinancePurchaseInvoiceId"";
    END LOOP;

    UPDATE public.sys_serial_number
    SET ""CurrentSequence"" = seq,
        ""UpdateTime"" = timezone('utc', now())
    WHERE ""ModuleCode"" = 'InputInvoice';
  END IF;
END $backfill$;

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_financepurchaseinvoice_InvoiceCode""
  ON public.financepurchaseinvoice (""InvoiceCode"")
  WHERE ""InvoiceCode"" IS NOT NULL AND btrim(""InvoiceCode"") <> '';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.""IX_financepurchaseinvoice_InvoiceCode"";
ALTER TABLE public.financepurchaseinvoice DROP COLUMN IF EXISTS ""InvoiceCode"";
");
        }
    }
}
