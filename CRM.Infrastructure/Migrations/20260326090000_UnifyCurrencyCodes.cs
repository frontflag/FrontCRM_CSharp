using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// Unify currency codes across modules (1-based):
    /// 1=RMB, 2=USD, 3=EUR, 4=HKD, 5=JPY, 6=GBP
    ///
    /// Includes a data migration for historical quoteitem.currency (0-based -> 1-based).
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260326090000_UnifyCurrencyCodes")]
    public partial class UnifyCurrencyCodes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1) Data migration: quoteitem.currency was historically 0-based (0..3).
--    Convert to unified 1-based codes (1..4).
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns c
        WHERE c.table_schema = 'public'
          AND c.table_name = 'quoteitem'
          AND c.column_name = 'currency'
    ) THEN
        UPDATE public.quoteitem
        SET ""currency"" = ""currency"" + 1
        WHERE ""currency"" BETWEEN 0 AND 3;
    END IF;
END $$;

-- 2) Column comments: align to unified currency codes everywhere.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='purchaseorder' AND column_name='currency') THEN
        EXECUTE 'COMMENT ON COLUMN public.purchaseorder.""currency"" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='purchaseorderitem' AND column_name='currency') THEN
        EXECUTE 'COMMENT ON COLUMN public.purchaseorderitem.""currency"" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='sellorder' AND column_name='currency') THEN
        EXECUTE 'COMMENT ON COLUMN public.sellorder.""currency"" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='sellorderitem' AND column_name='currency') THEN
        EXECUTE 'COMMENT ON COLUMN public.sellorderitem.""currency"" IS ''币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='rfqitem' AND column_name='price_currency') THEN
        EXECUTE 'COMMENT ON COLUMN public.rfqitem.""price_currency"" IS ''目标价币种：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='public' AND table_name='quoteitem' AND column_name='currency') THEN
        EXECUTE 'COMMENT ON COLUMN public.quoteitem.""currency"" IS ''报价币别：1RMB 2USD 3EUR 4HKD 5JPY 6GBP''';
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Best-effort rollback:
-- Only revert rows that still look like the unified range produced by the 0-based conversion.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns c
        WHERE c.table_schema = 'public'
          AND c.table_name = 'quoteitem'
          AND c.column_name = 'currency'
    ) THEN
        UPDATE public.quoteitem
        SET ""currency"" = ""currency"" - 1
        WHERE ""currency"" BETWEEN 1 AND 4;
    END IF;
END $$;
");
        }

        protected override void BuildTargetModel(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            // Intentionally empty: this migration only runs SQL for data/comments.
        }
    }
}

