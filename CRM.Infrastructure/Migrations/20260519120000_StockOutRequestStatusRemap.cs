using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <inheritdoc />
public partial class StockOutRequestStatusRemap : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE stockoutrequest
            SET status = CASE status
                WHEN 0 THEN 10
                WHEN 1 THEN 100
                WHEN 2 THEN -1
                ELSE status
            END
            WHERE status IN (0, 1, 2);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE stockoutrequest
            SET status = CASE status
                WHEN 10 THEN 0
                WHEN 100 THEN 1
                WHEN -1 THEN 2
                WHEN 20 THEN 0
                ELSE status
            END
            WHERE status IN (10, 20, 100, -1);
            """);
    }
}
