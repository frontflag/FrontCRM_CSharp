using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>我的邮件：本地星标（不写回 IMAP）。</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260923180000_AddUserMailStarred")]
public partial class AddUserMailStarred : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE public.user_mail_message
              ADD COLUMN IF NOT EXISTS is_starred boolean NOT NULL DEFAULT false;

            COMMENT ON COLUMN public.user_mail_message.is_starred IS '本地星标；同步不覆盖，不写回 IMAP';

            CREATE INDEX IF NOT EXISTS ix_user_mail_message_user_starred
              ON public.user_mail_message (user_id, is_starred)
              WHERE is_starred;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX IF EXISTS public.ix_user_mail_message_user_starred;
            ALTER TABLE public.user_mail_message DROP COLUMN IF EXISTS is_starred;
            """);
    }
}
