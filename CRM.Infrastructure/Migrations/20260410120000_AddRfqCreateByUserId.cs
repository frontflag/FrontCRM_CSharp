using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 需求主表增加 create_by_user_id（GUID），用于列表「创建人」展示；业务员名由列表接口联表解析。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260410120000_AddRfqCreateByUserId")]
    public partial class AddRfqCreateByUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 与 scripts/ensure_rfq_create_by_user_id_202604.sql 一致：列已存在（手工脚本或重复执行）时不报错
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public' AND table_name = 'rfq' AND column_name = 'create_by_user_id'
  ) THEN
    ALTER TABLE public.rfq ADD COLUMN create_by_user_id character varying(36) NULL;
  END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE IF EXISTS public.rfq DROP COLUMN IF EXISTS create_by_user_id;");
        }
    }
}
