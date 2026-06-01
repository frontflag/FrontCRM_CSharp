using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 为 public.stock 全部列补充 PostgreSQL 业务列注释。
/// 可重复执行脚本见 scripts/stock_column_business_comments_postgresql.sql。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260727120000_StockColumnBusinessComments")]
public partial class StockColumnBusinessComments : Migration
{
    private const string EmbeddedSqlResourceName =
        "CRM.Infrastructure.Migrations.Sql.StockColumnBusinessComments.sql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var assembly = typeof(StockColumnBusinessComments).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedSqlResourceName)
            ?? throw new InvalidOperationException(
                $"未找到嵌入式 SQL 资源 {EmbeddedSqlResourceName}，请确认 CRM.Infrastructure.csproj 已包含 EmbeddedResource。");
        using var reader = new StreamReader(stream);
        migrationBuilder.Sql(reader.ReadToEnd());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 列注释为文档元数据，不做自动回滚
    }
}
