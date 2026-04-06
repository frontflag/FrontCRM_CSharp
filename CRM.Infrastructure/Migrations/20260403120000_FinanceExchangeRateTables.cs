using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 财务汇率设置表 + 修改日志表（与全库脚本风格一致：PascalCase 列名）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260403120000_FinanceExchangeRateTables")]
    public partial class FinanceExchangeRateTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE financeexchangeratesetting (
    ""FinanceExchangeRateSettingId"" character varying(36) NOT NULL,
    ""UsdToCny"" numeric(12,4) NOT NULL,
    ""UsdToHkd"" numeric(12,4) NOT NULL,
    ""UsdToEur"" numeric(12,4) NOT NULL,
    ""EditorUserId"" character varying(36),
    ""EditorUserName"" character varying(100),
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""ModifyTime"" timestamp with time zone,
    CONSTRAINT ""PK_financeexchangeratesetting"" PRIMARY KEY (""FinanceExchangeRateSettingId"")
);

CREATE TABLE financeexchangeratechangelog (
    ""FinanceExchangeRateChangeLogId"" character varying(36) NOT NULL,
    ""UsdToCny"" numeric(12,4) NOT NULL,
    ""UsdToHkd"" numeric(12,4) NOT NULL,
    ""UsdToEur"" numeric(12,4) NOT NULL,
    ""ChangeUserId"" character varying(36),
    ""ChangeUserName"" character varying(100),
    ""ChangeSummary"" character varying(500),
    ""CreateTime"" timestamp with time zone NOT NULL,
    CONSTRAINT ""PK_financeexchangeratechangelog"" PRIMARY KEY (""FinanceExchangeRateChangeLogId"")
);

CREATE INDEX ""IX_financeexchangeratechangelog_CreateTime"" ON financeexchangeratechangelog (""CreateTime"");

INSERT INTO financeexchangeratesetting (""FinanceExchangeRateSettingId"", ""UsdToCny"", ""UsdToHkd"", ""UsdToEur"", ""EditorUserId"", ""EditorUserName"", ""CreateTime"", ""ModifyTime"")
VALUES ('00000000-0000-4000-8000-0000000000E1', 6.9194, 7.8367, 0.8725, NULL, NULL, NOW() AT TIME ZONE 'UTC', NULL);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS financeexchangeratechangelog;
DROP TABLE IF EXISTS financeexchangeratesetting;
");
        }
    }
}
