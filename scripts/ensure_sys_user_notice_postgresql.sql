-- 点对点系统通知（可重复执行）

CREATE TABLE IF NOT EXISTS public.sys_user_notice (
  id character varying(36) NOT NULL,
  recipient_user_id character varying(36) NOT NULL,
  is_urgent boolean NOT NULL DEFAULT false,
  title character varying(100) NOT NULL,
  body character varying(4000) NOT NULL DEFAULT '',
  sender_user_id character varying(36) NOT NULL,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  read_at timestamp with time zone NULL,
  CONSTRAINT "PK_sys_user_notice" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_sys_user_notice_recipient_create
  ON public.sys_user_notice (recipient_user_id, create_time DESC);

CREATE INDEX IF NOT EXISTS ix_sys_user_notice_create_time
  ON public.sys_user_notice (create_time DESC);

CREATE INDEX IF NOT EXISTS ix_sys_user_notice_unread
  ON public.sys_user_notice (recipient_user_id)
  WHERE read_at IS NULL;

COMMENT ON TABLE public.sys_user_notice IS '点对点系统通知；发送即落库，read_at 为空表示未读';
