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
  like_count integer NOT NULL DEFAULT 0,
  dislike_count integer NOT NULL DEFAULT 0,
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
  like_count integer NOT NULL DEFAULT 0,
  dislike_count integer NOT NULL DEFAULT 0,
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

-- 赞/踩（主题=1，回复=2；value: 1赞 -1踩）
CREATE TABLE IF NOT EXISTS public.bbs_reaction (
  id character varying(36) NOT NULL,
  target_type integer NOT NULL,
  target_id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  value integer NOT NULL,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  modify_time timestamp with time zone NULL,
  CONSTRAINT "PK_bbs_reaction" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_bbs_reaction_target_user
  ON public.bbs_reaction (target_type, target_id, user_id);

ALTER TABLE public.bbs_subject ADD COLUMN IF NOT EXISTS like_count integer NOT NULL DEFAULT 0;
ALTER TABLE public.bbs_subject ADD COLUMN IF NOT EXISTS dislike_count integer NOT NULL DEFAULT 0;
ALTER TABLE public.bbs_reply ADD COLUMN IF NOT EXISTS like_count integer NOT NULL DEFAULT 0;
ALTER TABLE public.bbs_reply ADD COLUMN IF NOT EXISTS dislike_count integer NOT NULL DEFAULT 0;

-- 板块设置（版主可空；可自定义显示名）
CREATE TABLE IF NOT EXISTS public.bbs_board_moderator (
  subject_type integer NOT NULL,
  user_id character varying(36) NULL,
  display_name character varying(50) NULL,
  update_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  update_by character varying(36) NULL,
  CONSTRAINT "PK_bbs_board_moderator" PRIMARY KEY (subject_type)
);

ALTER TABLE public.bbs_board_moderator ALTER COLUMN user_id DROP NOT NULL;
ALTER TABLE public.bbs_board_moderator ADD COLUMN IF NOT EXISTS display_name character varying(50) NULL;
ALTER TABLE public.bbs_board_moderator ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
ALTER TABLE public.bbs_board_moderator ADD COLUMN IF NOT EXISTS sort_order integer NOT NULL DEFAULT 0;

-- 仅当尚未使用 10+ 间距时，将旧默认 1/2/3/4 拉开
DO $bbs_sort$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM public.bbs_board_moderator WHERE sort_order >= 10
  ) THEN
    UPDATE public.bbs_board_moderator SET sort_order = 10 WHERE subject_type = 1 AND sort_order = 1;
    UPDATE public.bbs_board_moderator SET sort_order = 20 WHERE subject_type = 2 AND sort_order = 2;
    UPDATE public.bbs_board_moderator SET sort_order = 30 WHERE subject_type = 3 AND sort_order = 3;
    UPDATE public.bbs_board_moderator SET sort_order = 40 WHERE subject_type = 5 AND sort_order = 4;
  END IF;
END
$bbs_sort$;

CREATE INDEX IF NOT EXISTS ix_bbs_board_moderator_user
  ON public.bbs_board_moderator (user_id);

COMMENT ON TABLE public.bbs_board_moderator IS '论坛板块设置；subject_type 对应 bbs_subject.type；可设版主与自定义名称；仅 SYS_ADMIN/SYS_MANAGER 可设置';

-- 投票帖字段与表
ALTER TABLE public.bbs_subject ADD COLUMN IF NOT EXISTS kind integer NOT NULL DEFAULT 0;
ALTER TABLE public.bbs_subject ADD COLUMN IF NOT EXISTS vote_mode integer NOT NULL DEFAULT 0;
ALTER TABLE public.bbs_subject ADD COLUMN IF NOT EXISTS vote_max_choices integer NULL;
ALTER TABLE public.bbs_subject ADD COLUMN IF NOT EXISTS vote_deadline timestamp with time zone NULL;
ALTER TABLE public.bbs_subject ADD COLUMN IF NOT EXISTS vote_count integer NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS public.bbs_poll_option (
  id character varying(36) NOT NULL,
  subject_id character varying(36) NOT NULL,
  sort_order integer NOT NULL DEFAULT 0,
  text character varying(100) NOT NULL,
  is_deleted boolean NOT NULL DEFAULT false,
  CONSTRAINT "PK_bbs_poll_option" PRIMARY KEY (id)
);
CREATE INDEX IF NOT EXISTS ix_bbs_poll_option_subject
  ON public.bbs_poll_option (subject_id, sort_order);

CREATE TABLE IF NOT EXISTS public.bbs_poll_vote (
  id character varying(36) NOT NULL,
  subject_id character varying(36) NOT NULL,
  user_id character varying(36) NOT NULL,
  create_time timestamp with time zone NOT NULL DEFAULT (timezone('utc', now())),
  CONSTRAINT "PK_bbs_poll_vote" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_bbs_poll_vote_subject_user
  ON public.bbs_poll_vote (subject_id, user_id);

CREATE TABLE IF NOT EXISTS public.bbs_poll_vote_item (
  id character varying(36) NOT NULL,
  vote_id character varying(36) NOT NULL,
  option_id character varying(36) NOT NULL,
  CONSTRAINT "PK_bbs_poll_vote_item" PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_bbs_poll_vote_item
  ON public.bbs_poll_vote_item (vote_id, option_id);
CREATE INDEX IF NOT EXISTS ix_bbs_poll_vote_item_option
  ON public.bbs_poll_vote_item (option_id);

-- 可选：全局版主权限码 bbs.moderate（过渡；SYS_ADMIN/SYS_MANAGER 已内置全板块版主能力）
-- INSERT INTO public.permission (id, code, name, ...) ...
