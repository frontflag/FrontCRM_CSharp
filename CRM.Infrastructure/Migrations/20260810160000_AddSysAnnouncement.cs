using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260810160000_AddSysAnnouncement")]
    public partial class AddSysAnnouncement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.sys_announcement (
  id character varying(36) NOT NULL,
  title character varying(100) NOT NULL,
  type character varying(32) NOT NULL DEFAULT 'platform_notice',
  body_md text NOT NULL DEFAULT '',
  status character varying(16) NOT NULL DEFAULT 'draft',
  published_at timestamp with time zone NULL,
  published_by character varying(36) NULL,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  create_by character varying(36) NULL,
  modify_time timestamp with time zone NULL,
  modify_by character varying(36) NULL,
  CONSTRAINT ""PK_sys_announcement"" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_sys_announcement_status_published_at
  ON public.sys_announcement (status, published_at DESC NULLS LAST);

CREATE TABLE IF NOT EXISTS public.sys_announcement_read (
  id character varying(36) NOT NULL,
  announcement_id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  read_at timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  CONSTRAINT ""PK_sys_announcement_read"" PRIMARY KEY (id),
  CONSTRAINT ""FK_sys_announcement_read_announcement""
    FOREIGN KEY (announcement_id) REFERENCES public.sys_announcement (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_sys_announcement_read_ann_user
  ON public.sys_announcement_read (announcement_id, user_id);

CREATE INDEX IF NOT EXISTS ix_sys_announcement_read_user_id
  ON public.sys_announcement_read (user_id);

COMMENT ON TABLE public.sys_announcement IS '系统公告（草稿/已发布）';
COMMENT ON TABLE public.sys_announcement_read IS '系统公告已读记录；未读=已发布且无本行';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.sys_announcement_read;
DROP TABLE IF EXISTS public.sys_announcement;
");
        }
    }
}
