using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260810130000_AddUserMailboxDefaultSend")]
    public partial class AddUserMailboxDefaultSend : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.user_mailbox
  ADD COLUMN IF NOT EXISTS is_default_send boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.user_mailbox.is_default_send IS '默认发信；同一用户未删除行中至多一条为 true';

CREATE UNIQUE INDEX IF NOT EXISTS ux_user_mailbox_default_send
  ON public.user_mailbox (user_id)
  WHERE is_default_send AND NOT is_deleted;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS public.ux_user_mailbox_default_send;
ALTER TABLE public.user_mailbox DROP COLUMN IF EXISTS is_default_send;
");
        }
    }
}
