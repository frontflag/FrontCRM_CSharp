using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260810120000_AddUserMailbox")]
    public partial class AddUserMailbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.user_mailbox (
  id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  kind smallint NOT NULL DEFAULT 0,
  address character varying(256) NOT NULL,
  local_part character varying(128) NULL,
  display_name character varying(200) NULL,
  password_cipher text NULL,
  crypto_version smallint NOT NULL DEFAULT 1,
  pop_host character varying(256) NULL,
  pop_port integer NULL,
  pop_use_ssl boolean NOT NULL DEFAULT true,
  verify_status smallint NOT NULL DEFAULT 0,
  verify_message character varying(1000) NULL,
  verified_at timestamp with time zone NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  modify_time timestamp with time zone NULL,
  create_by_user_id character varying(36) NULL,
  modify_by_user_id character varying(36) NULL,
  CONSTRAINT ""PK_user_mailbox"" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_user_mailbox_user_id ON public.user_mailbox (user_id);
CREATE INDEX IF NOT EXISTS ix_user_mailbox_verify_status ON public.user_mailbox (verify_status)
  WHERE NOT is_deleted;

COMMENT ON TABLE public.user_mailbox IS '用户个人邮箱（平台/其他），密码对称加密';
COMMENT ON COLUMN public.user_mailbox.kind IS '0=平台邮箱 1=其他邮箱';
COMMENT ON COLUMN public.user_mailbox.verify_status IS '0=未验证 1=成功 2=失败';
COMMENT ON COLUMN public.user_mailbox.password_cipher IS '对称加密密文，可解密';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.user_mailbox;");
        }
    }
}
