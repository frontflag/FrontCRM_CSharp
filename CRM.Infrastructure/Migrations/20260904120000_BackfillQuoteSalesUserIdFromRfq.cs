using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 回写报价头业务员：空值，或被写成采购员且需求主表业务员不同时，改为需求 <c>rfq.sales_user_id</c>。
/// 需求业务员为空、或需求上业务员本就与采购员同一人时不改。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260904120000_BackfillQuoteSalesUserIdFromRfq")]
public partial class BackfillQuoteSalesUserIdFromRfq : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public.quote q
            SET sales_user_id = btrim(r.sales_user_id),
                "ModifyTime" = NOW()
            FROM public.rfq r
            WHERE NOT q.is_deleted
              AND NOT r.is_deleted
              AND q.rfq_id IS NOT NULL
              AND btrim(q.rfq_id) <> ''
              AND r.rfq_id = q.rfq_id
              AND r.sales_user_id IS NOT NULL
              AND btrim(r.sales_user_id) <> ''
              AND (
                    q.sales_user_id IS NULL
                    OR btrim(q.sales_user_id) = ''
                    OR (
                        q.purchase_user_id IS NOT NULL
                        AND btrim(q.purchase_user_id) <> ''
                        AND lower(btrim(q.sales_user_id)) = lower(btrim(q.purchase_user_id))
                        AND lower(btrim(r.sales_user_id)) <> lower(btrim(q.purchase_user_id))
                    )
                  );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 历史错值无法无损还原，不回滚数据。
    }
}
