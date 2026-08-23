using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>我的邮件：本地备注（不写回 IMAP）。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260923190000_AddUserMailRemark")]
public partial class AddUserMailRemark : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.user_mail_message
              ADD COLUMN IF NOT EXISTS remark character varying(2000) NULL;

            COMMENT ON COLUMN public.user_mail_message.remark IS '本地备注；同步不覆盖，不写回 IMAP';

            CREATE INDEX IF NOT EXISTS ix_user_mail_message_user_remark
              ON public.user_mail_message (user_id)
              WHERE remark IS NOT NULL AND btrim(remark) <> '';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public.ix_user_mail_message_user_remark;
            ALTER TABLE public.user_mail_message DROP COLUMN IF EXISTS remark;
            """);
    }
}
