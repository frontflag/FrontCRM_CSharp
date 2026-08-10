using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260810180000_AddUserMailInbox")]
    public partial class AddUserMailInbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE public.user_mailbox
  ADD COLUMN IF NOT EXISTS imap_host character varying(256) NULL,
  ADD COLUMN IF NOT EXISTS imap_port integer NULL,
  ADD COLUMN IF NOT EXISTS imap_use_ssl boolean NOT NULL DEFAULT true;

COMMENT ON COLUMN public.user_mailbox.imap_host IS '个人邮箱 IMAP 主机；平台邮箱用公司设置';
COMMENT ON COLUMN public.user_mailbox.imap_port IS 'IMAP 端口，默认 993';
COMMENT ON COLUMN public.user_mailbox.imap_use_ssl IS 'IMAP 是否 SSL';

CREATE TABLE IF NOT EXISTS public.user_mail_message (
  id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  mailbox_id character varying(36) NOT NULL,
  imap_uid bigint NOT NULL,
  folder character varying(128) NOT NULL DEFAULT 'INBOX',
  message_id character varying(998) NULL,
  subject character varying(1000) NULL,
  from_address character varying(512) NULL,
  from_name character varying(256) NULL,
  to_addresses text NULL,
  received_at timestamp with time zone NULL,
  is_unread boolean NOT NULL DEFAULT true,
  snippet character varying(500) NULL,
  body_text text NULL,
  body_html text NULL,
  has_attachments boolean NOT NULL DEFAULT false,
  size_bytes integer NOT NULL DEFAULT 0,
  is_deleted boolean NOT NULL DEFAULT false,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  modify_time timestamp with time zone NULL,
  CONSTRAINT ""PK_user_mail_message"" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_user_mail_message_mailbox_folder_uid
  ON public.user_mail_message (mailbox_id, folder, imap_uid)
  WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_user_mail_message_user_received
  ON public.user_mail_message (user_id, received_at DESC NULLS LAST)
  WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS ix_user_mail_message_user_unread
  ON public.user_mail_message (user_id, is_unread)
  WHERE NOT is_deleted;

COMMENT ON TABLE public.user_mail_message IS '我的邮件：IMAP 同步落库（一期 INBOX）';

CREATE TABLE IF NOT EXISTS public.user_mailbox_sync_state (
  mailbox_id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  last_sync_at timestamp with time zone NULL,
  last_success_at timestamp with time zone NULL,
  last_error character varying(2000) NULL,
  last_uid_validity bigint NULL,
  CONSTRAINT ""PK_user_mailbox_sync_state"" PRIMARY KEY (mailbox_id)
);

CREATE INDEX IF NOT EXISTS ix_user_mailbox_sync_state_user_id
  ON public.user_mailbox_sync_state (user_id);

COMMENT ON TABLE public.user_mailbox_sync_state IS '每邮箱 IMAP 同步状态';

CREATE TABLE IF NOT EXISTS public.mail_sync_daily_run (
  run_date character varying(10) NOT NULL,
  started_at timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  finished_at timestamp with time zone NULL,
  ok_count integer NOT NULL DEFAULT 0,
  fail_count integer NOT NULL DEFAULT 0,
  CONSTRAINT ""PK_mail_sync_daily_run"" PRIMARY KEY (run_date)
);

COMMENT ON TABLE public.mail_sync_daily_run IS '每日 08:30 自动同步跑批标记（Asia/Shanghai 日期）';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.mail_sync_daily_run;
DROP TABLE IF EXISTS public.user_mailbox_sync_state;
DROP TABLE IF EXISTS public.user_mail_message;
ALTER TABLE public.user_mailbox
  DROP COLUMN IF EXISTS imap_host,
  DROP COLUMN IF EXISTS imap_port,
  DROP COLUMN IF EXISTS imap_use_ssl;
");
        }
    }
}
