using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 出库通知改单表：字段并入 stockoutrequest，删除 stockoutrequestitem；历史数据从子表首行回填。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260325183000_StockOutRequestFlattenSingleTable")]
    public partial class StockOutRequestFlattenSingleTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE IF EXISTS public.stockoutrequest
  ADD COLUMN IF NOT EXISTS ""SalesOrderItemId"" character varying(36) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS public.stockoutrequest
  ADD COLUMN IF NOT EXISTS ""MaterialCode"" character varying(200) NOT NULL DEFAULT '';
ALTER TABLE IF EXISTS public.stockoutrequest
  ADD COLUMN IF NOT EXISTS ""MaterialName"" character varying(200);
ALTER TABLE IF EXISTS public.stockoutrequest
  ADD COLUMN IF NOT EXISTS ""Quantity"" numeric(18,4) NOT NULL DEFAULT 0;
");

            migrationBuilder.Sql(@"
DO $BODY$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.tables
    WHERE table_schema = 'public' AND table_name = 'stockoutrequestitem'
  ) THEN
    UPDATE public.stockoutrequest r
    SET
      ""MaterialCode"" = COALESCE(sub.""MaterialCode"", ''),
      ""MaterialName"" = sub.""MaterialName"",
      ""Quantity"" = sub.""Quantity""
    FROM (
      SELECT DISTINCT ON (""StockOutRequestId"")
        ""StockOutRequestId"",
        ""LineNo"",
        ""MaterialCode"",
        ""MaterialName"",
        ""Quantity""
      FROM public.stockoutrequestitem
      ORDER BY ""StockOutRequestId"", ""LineNo""
    ) AS sub
    WHERE r.""UserId"" = sub.""StockOutRequestId"";

    DROP TABLE IF EXISTS public.stockoutrequestitem CASCADE;
  END IF;
END $BODY$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
