using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 移除已废弃的 SalesOrderLineNo 列（若存在）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260325200000_DropStockOutRequestSalesOrderLineNo")]
    public partial class DropStockOutRequestSalesOrderLineNo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE IF EXISTS public.stockoutrequest DROP COLUMN IF EXISTS ""SalesOrderLineNo"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
