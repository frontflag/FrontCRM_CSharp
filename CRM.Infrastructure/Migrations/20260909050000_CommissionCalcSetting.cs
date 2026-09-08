using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>提成计算全局参数：收款核销延期提成天数。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260909050000_CommissionCalcSetting")]
public partial class CommissionCalcSetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS public.commission_calc_setting (
              id character varying(36) NOT NULL,
              receipt_writeoff_delay_days integer NOT NULL DEFAULT 0,
              created_at timestamp with time zone NOT NULL DEFAULT NOW(),
              updated_at timestamp with time zone NOT NULL DEFAULT NOW(),
              updated_by character varying(36) NULL,
              CONSTRAINT "PK_commission_calc_setting" PRIMARY KEY (id),
              CONSTRAINT "CK_commission_calc_setting_delay_days"
                CHECK (receipt_writeoff_delay_days BETWEEN 0 AND 3650)
            );

            INSERT INTO public.commission_calc_setting (id, receipt_writeoff_delay_days, created_at, updated_at)
            VALUES ('c2000000-0000-4000-8000-000000000010', 0, NOW(), NOW())
            ON CONFLICT (id) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS public.commission_calc_setting;
            """);
    }
}
