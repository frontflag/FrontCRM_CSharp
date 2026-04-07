using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 库存流水物理表 <c>inventoryledger</c> 重命名为 <c>stockledger</c>（实体仍为 <see cref="CRM.Core.Models.Inventory.InventoryLedger"/>）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260426120000_RenameInventoryLedgerToStockLedger")]
    public partial class RenameInventoryLedgerToStockLedger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.inventoryledger RENAME TO stockledger;

DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_inventoryledger_BizKey') THEN
    EXECUTE 'ALTER INDEX public.""IX_inventoryledger_BizKey"" RENAME TO ""IX_stockledger_BizType_BizId_BizLineId""';
  END IF;
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_inventoryledger_BizType_BizId_BizLineId') THEN
    EXECUTE 'ALTER INDEX public.""IX_inventoryledger_BizType_BizId_BizLineId"" RENAME TO ""IX_stockledger_BizType_BizId_BizLineId""';
  END IF;
END $$;

DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM pg_constraint c
    JOIN pg_class t ON t.oid = c.conrelid
    JOIN pg_namespace n ON n.oid = t.relnamespace
    WHERE n.nspname = 'public' AND t.relname = 'stockledger' AND c.conname = 'PK_inventoryledger') THEN
    EXECUTE 'ALTER TABLE public.stockledger RENAME CONSTRAINT ""PK_inventoryledger"" TO ""PK_stockledger""';
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
    SELECT 1 FROM pg_constraint c
    JOIN pg_class t ON t.oid = c.conrelid
    JOIN pg_namespace n ON n.oid = t.relnamespace
    WHERE n.nspname = 'public' AND t.relname = 'stockledger' AND c.conname = 'PK_stockledger') THEN
    EXECUTE 'ALTER TABLE public.stockledger RENAME CONSTRAINT ""PK_stockledger"" TO ""PK_inventoryledger""';
  END IF;
END $$;

DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_stockledger_BizType_BizId_BizLineId') THEN
    EXECUTE 'ALTER INDEX public.""IX_stockledger_BizType_BizId_BizLineId"" RENAME TO ""IX_inventoryledger_BizKey""';
  END IF;
END $$;

ALTER TABLE IF EXISTS public.stockledger RENAME TO inventoryledger;
");
        }
    }
}
