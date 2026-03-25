using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260325120000_AddStockOutRequestItem")]
    public partial class AddStockOutRequestItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.stockoutrequestitem (
    ""UserId"" character varying(36) NOT NULL,
    ""StockOutRequestId"" character varying(36) NOT NULL,
    ""LineNo"" integer NOT NULL,
    ""MaterialCode"" character varying(200) NOT NULL,
    ""MaterialName"" character varying(200),
    ""Quantity"" numeric(18,4) NOT NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint,
    ""ModifyTime"" timestamp with time zone,
    ""ModifyUserId"" bigint,
    CONSTRAINT ""PK_stockoutrequestitem"" PRIMARY KEY (""UserId"")
);

CREATE INDEX IF NOT EXISTS ""IX_stockoutrequestitem_StockOutRequestId"" ON public.stockoutrequestitem (""StockOutRequestId"");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_stockoutrequestitem_stockoutrequest_StockOutRequestId') THEN
        ALTER TABLE public.stockoutrequestitem
        ADD CONSTRAINT ""FK_stockoutrequestitem_stockoutrequest_StockOutRequestId""
        FOREIGN KEY (""StockOutRequestId"") REFERENCES public.stockoutrequest (""UserId"") ON DELETE CASCADE;
    END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.stockoutrequestitem;");
        }
    }
}
