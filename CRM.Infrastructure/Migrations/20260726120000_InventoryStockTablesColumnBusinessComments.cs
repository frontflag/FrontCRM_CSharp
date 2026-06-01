using System.Reflection;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 为库存/入库/出库/移库/通知相关表全部列补充 PostgreSQL 业务列注释。
/// 可重复执行脚本见 scripts/inventory_stock_tables_column_business_comments_postgresql.sql。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260726120000_InventoryStockTablesColumnBusinessComments")]
public partial class InventoryStockTablesColumnBusinessComments : Migration
{
    private const string EmbeddedSqlResourceName =
        "CRM.Infrastructure.Migrations.Sql.InventoryStockTablesColumnBusinessComments.sql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(InventoryStockTablesColumnBusinessComments).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedSqlResourceName)
            ?? throw new InvalidOperationException(
                $"未找到嵌入式 SQL 资源 {EmbeddedSqlResourceName}，请确认 CRM.Infrastructure.csproj 已包含 EmbeddedResource。");
        using var reader = new StreamReader(stream);
        var sql = reader.ReadToEnd();
        migrationBuilder.Sql(sql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 列注释为文档元数据，不做自动回滚
    }
}
