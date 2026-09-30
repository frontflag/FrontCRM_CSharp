-- BBS 论坛 MVP：主题 + 回复（幂等）
-- 对照：document/System/协作/BBS论坛-设计与实现.md

CREATE TABLE IF NOT EXISTS public.bbs_subject (
  id character varying(36) NOT NULL,
  title character varying(200) NOT NULL,
  content text NOT NULL,
  type integer NOT NULL,
  status integer NOT NULL DEFAULT 1,
  is_top boolean NOT NULL DEFAULT false,
  is_hot boolean NOT NULL DEFAULT false,
  anonymous boolean NOT NULL DEFAULT false,
  view_count integer NOT NULL DEFAULT 0,
  reply_count integer NOT NULL DEFAULT 0,
  last_reply_time timestamp with time zone NULL,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  create_by character varying(36) NULL,
  modify_time timestamp with time zone NULL,
  modify_by character varying(36) NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  CONSTRAINT "PK_bbs_subject" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_bbs_subject_list
  ON public.bbs_subject (is_deleted, is_top, last_reply_time DESC NULLS LAST, create_time DESC);

CREATE INDEX IF NOT EXISTS ix_bbs_subject_type_status
  ON public.bbs_subject (type, status)
  WHERE is_deleted = false;

COMMENT ON TABLE public.bbs_subject IS '内部论坛主题；type: 1公司通告 2行业快讯 3交流分享 4操作说明 5优化建议 6系统更新';

CREATE TABLE IF NOT EXISTS public.bbs_reply (
  id character varying(36) NOT NULL,
  subject_id character varying(36) NOT NULL,
  content text NOT NULL,
  anonymous boolean NOT NULL DEFAULT false,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  create_by character varying(36) NULL,
  modify_time timestamp with time zone NULL,
  modify_by character varying(36) NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  CONSTRAINT "PK_bbs_reply" PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_bbs_reply_subject
  ON public.bbs_reply (subject_id, create_time)
  WHERE is_deleted = false;

COMMENT ON TABLE public.bbs_reply IS '内部论坛回复';

-- 可选：版主权限码（按需赋给角色；SYS_ADMIN/SYS_MANAGER 已内置版主能力）
-- INSERT INTO public.permission (id, code, name, ...) ...
