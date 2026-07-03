using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 报价主状态三态化：0新建 1成单 2关闭；迁移旧 1–7 编码。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260731180000_NormalizeQuoteMainStatusThreeState")]
public partial class NormalizeQuoteMainStatusThreeState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
-- 1) 有有效销售订单引用的报价 → 成单(1)
UPDATE quote q
SET status = 1, ""ModifyTime"" = NOW()
WHERE NOT q.is_deleted
  AND EXISTS (
    SELECT 1
    FROM sellorderitem si
    INNER JOIN sellorder so ON so.""SellOrderId"" = si.sell_order_id
    WHERE si.quote_id = q.""QuoteId""
      AND NOT si.is_deleted
      AND NOT so.is_deleted
      AND so.status <> -2
  );

-- 2) 旧「已关闭」(7) → 关闭(2)，已成单(1) 不动
UPDATE quote
SET status = 2, ""ModifyTime"" = NOW()
WHERE NOT is_deleted AND status = 7;

-- 3) 无需求明细的历史脏数据 → 关闭(2)
UPDATE quote
SET status = 2, ""ModifyTime"" = NOW()
WHERE NOT is_deleted
  AND (rfq_item_id IS NULL OR TRIM(rfq_item_id) = '')
  AND status <> 1;

-- 4) 其余旧编码(1–6 等) → 新建(0)
UPDATE quote
SET status = 0, ""ModifyTime"" = NOW()
WHERE NOT is_deleted AND status NOT IN (0, 1, 2);

COMMENT ON COLUMN public.quote.status IS '状态：0新建 1成单 2关闭';
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
COMMENT ON COLUMN public.quote.status IS '状态：0草稿 1待审核 2已审核 3已发送 4已接受 5已拒绝 6已过期 7已关闭';
");
    }
}
