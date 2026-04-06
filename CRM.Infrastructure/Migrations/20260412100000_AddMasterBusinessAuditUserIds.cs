using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 主业务表统一增加 create_by_user_id / modify_by_user_id（GUID）。rfq 仅补 modify；sellorder/purchaseorder 已有列跳过；paymentrequest 表若不存在则跳过。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260412100000_AddMasterBusinessAuditUserIds")]
    public partial class AddMasterBusinessAuditUserIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "customerinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "customerinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "vendorinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "vendorinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "rfq",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "quote",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "quote",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "purchaserequisition",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "purchaserequisition",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "stockoutrequest",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "stockoutrequest",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "qcinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "qcinfo",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "stockin",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "stockin",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "stockout",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "stockout",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "financereceipt",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "financereceipt",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "financepayment",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "financepayment",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "financepurchaseinvoice",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "financepurchaseinvoice",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "create_by_user_id",
                table: "financesellinvoice",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "modify_by_user_id",
                table: "financesellinvoice",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF to_regclass('public.paymentrequest') IS NOT NULL THEN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'paymentrequest' AND column_name = 'create_by_user_id') THEN
      ALTER TABLE public.paymentrequest ADD COLUMN create_by_user_id character varying(36) NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'paymentrequest' AND column_name = 'modify_by_user_id') THEN
      ALTER TABLE public.paymentrequest ADD COLUMN modify_by_user_id character varying(36) NULL;
    END IF;
  END IF;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF to_regclass('public.paymentrequest') IS NOT NULL THEN
    ALTER TABLE public.paymentrequest DROP COLUMN IF EXISTS modify_by_user_id;
    ALTER TABLE public.paymentrequest DROP COLUMN IF EXISTS create_by_user_id;
  END IF;
END $$;
");

            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "financesellinvoice");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "financesellinvoice");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "financepurchaseinvoice");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "financepurchaseinvoice");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "financepayment");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "financepayment");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "financereceipt");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "financereceipt");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "stockout");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "stockout");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "stockin");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "stockin");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "qcinfo");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "qcinfo");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "stockoutrequest");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "stockoutrequest");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "purchaserequisition");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "purchaserequisition");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "quote");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "quote");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "rfq");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "vendorinfo");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "vendorinfo");
            migrationBuilder.DropColumn(name: "modify_by_user_id", table: "customerinfo");
            migrationBuilder.DropColumn(name: "create_by_user_id", table: "customerinfo");
        }
    }
}
