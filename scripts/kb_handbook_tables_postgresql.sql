-- 增量：知识库问答（建表 + 向量厂商 + 场景 + 权限）
-- DBeaver-safe：提示词占位符用 CHR 拼接，文件中不写双花括号字面量
-- 依赖：PostgreSQL 已安装 pgvector。可重复执行。
-- 生产只执行本文件，不必再单独跑 kb_handbook_scenario_postgresql.sql。

CREATE EXTENSION IF NOT EXISTS vector;

-- ========== kb_document ==========

CREATE TABLE IF NOT EXISTS public.kb_document (
    id                  character varying(36)  NOT NULL,
    code                character varying(100) NOT NULL,
    title               character varying(200) NOT NULL,
    active_version_id   character varying(36)  NULL,
    create_time         timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time         timestamp with time zone NULL,
    is_deleted          boolean                NOT NULL DEFAULT false,
    CONSTRAINT "PK_kb_document" PRIMARY KEY (id),
    CONSTRAINT "UX_kb_document_code" UNIQUE (code)
);

-- ========== kb_document_version ==========

CREATE TABLE IF NOT EXISTS public.kb_document_version (
    id                       character varying(36)  NOT NULL,
    document_id              character varying(36)  NOT NULL,
    version_no               integer                NOT NULL,
    source_file_name         character varying(260) NOT NULL,
    source_sha256            character varying(64)  NOT NULL,
    storage_path             character varying(500) NOT NULL DEFAULT '',
    embedding_provider_code  character varying(64)  NOT NULL DEFAULT '',
    embedding_model          character varying(100) NOT NULL DEFAULT '',
    embedding_dimension      integer                NOT NULL DEFAULT 1024,
    chunk_count              integer                NOT NULL DEFAULT 0,
    status                   smallint               NOT NULL DEFAULT 0,
    is_active                boolean                NOT NULL DEFAULT false,
    error_message            text                   NULL,
    create_time              timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time              timestamp with time zone NULL,
    is_deleted               boolean                NOT NULL DEFAULT false,
    CONSTRAINT "PK_kb_document_version" PRIMARY KEY (id),
    CONSTRAINT "UX_kb_document_version_no" UNIQUE (document_id, version_no),
    CONSTRAINT "CK_kb_document_version_dimension" CHECK (embedding_dimension = 1024),
    CONSTRAINT "CK_kb_document_version_status" CHECK (status BETWEEN 0 AND 4)
);

-- ========== kb_chunk ==========

CREATE TABLE IF NOT EXISTS public.kb_chunk (
    id                    character varying(36)  NOT NULL,
    document_version_id   character varying(36)  NOT NULL,
    chunk_index           integer                NOT NULL,
    chapter_no            character varying(20)  NULL,
    chapter_title         text                   NULL,
    section_no            character varying(20)  NULL,
    section_title         text                   NULL,
    heading               text                   NOT NULL DEFAULT '',
    content               text                   NOT NULL,
    content_chars         integer                NOT NULL DEFAULT 0,
    content_sha256        character varying(64)  NOT NULL,
    embedding             vector(1024)           NULL,
    create_time           timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT "PK_kb_chunk" PRIMARY KEY (id),
    CONSTRAINT "UX_kb_chunk_version_index" UNIQUE (document_version_id, chunk_index)
);

-- ========== kb_ask_cache ==========

CREATE TABLE IF NOT EXISTS public.kb_ask_cache (
    id                    character varying(36)  NOT NULL,
    document_version_id   character varying(36)  NOT NULL,
    question_sha256       character varying(64)  NOT NULL,
    question_norm         character varying(500) NOT NULL,
    covered               boolean                NOT NULL,
    answer                text                   NOT NULL DEFAULT '',
    chunk_ids             jsonb                  NOT NULL DEFAULT '[]'::jsonb,
    top_distance          double precision       NULL,
    expire_time           timestamp with time zone NOT NULL,
    create_time           timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT "PK_kb_ask_cache" PRIMARY KEY (id),
    CONSTRAINT "UX_kb_ask_cache_version_question" UNIQUE (document_version_id, question_sha256)
);

-- ========== kb_embedding_profile ==========

CREATE TABLE IF NOT EXISTS public.kb_embedding_profile (
    id              character varying(36)  NOT NULL,
    provider_code   character varying(64)  NOT NULL DEFAULT '',
    model           character varying(100) NOT NULL DEFAULT '',
    dimension       integer                NOT NULL DEFAULT 1024,
    is_enabled      boolean                NOT NULL DEFAULT false,
    create_time     timestamp with time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    modify_time     timestamp with time zone NULL,
    is_deleted      boolean                NOT NULL DEFAULT false,
    CONSTRAINT "PK_kb_embedding_profile" PRIMARY KEY (id),
    CONSTRAINT "CK_kb_embedding_profile_dimension" CHECK (dimension = 1024)
);

-- ========== foreign keys ==========

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_kb_document_version_document'
    ) THEN
        ALTER TABLE public.kb_document_version
            ADD CONSTRAINT "FK_kb_document_version_document"
            FOREIGN KEY (document_id) REFERENCES public.kb_document (id);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_kb_document_active_version'
    ) THEN
        ALTER TABLE public.kb_document
            ADD CONSTRAINT "FK_kb_document_active_version"
            FOREIGN KEY (active_version_id) REFERENCES public.kb_document_version (id);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_kb_chunk_version'
    ) THEN
        ALTER TABLE public.kb_chunk
            ADD CONSTRAINT "FK_kb_chunk_version"
            FOREIGN KEY (document_version_id) REFERENCES public.kb_document_version (id);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_kb_ask_cache_version'
    ) THEN
        ALTER TABLE public.kb_ask_cache
            ADD CONSTRAINT "FK_kb_ask_cache_version"
            FOREIGN KEY (document_version_id) REFERENCES public.kb_document_version (id);
    END IF;
END $$;

-- ========== indexes ==========

CREATE UNIQUE INDEX IF NOT EXISTS "UX_kb_document_version_one_active"
    ON public.kb_document_version (document_id)
    WHERE is_active = true AND is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_kb_document_version_ready_sha"
    ON public.kb_document_version (document_id, source_sha256)
    WHERE status = 2 AND is_deleted = false;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_kb_embedding_profile_singleton"
    ON public.kb_embedding_profile ((true))
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS "IX_kb_chunk_embedding_hnsw"
    ON public.kb_chunk USING hnsw (embedding vector_cosine_ops);

-- 已按旧脚本建成 varchar(200) 的库：标题可能超过 200 字，扩成 text。已是 text 时再执行无影响。
ALTER TABLE public.kb_chunk ALTER COLUMN chapter_title TYPE text;
ALTER TABLE public.kb_chunk ALTER COLUMN section_title TYPE text;
ALTER TABLE public.kb_chunk ALTER COLUMN heading TYPE text;

-- ========== 向量厂商、场景、权限 ==========

INSERT INTO public.ai_provider (
    id, code, name, base_url, api_key_env, default_model, timeout_seconds, is_enabled
)
VALUES (
    'a1000001-0000-4000-8000-0000000000b1',
    'siliconflow',
    'SiliconFlow',
    'https://api.siliconflow.cn/v1',
    'AI_EMBEDDING_API_KEY',
    'BAAI/bge-m3',
    120,
    true
)
ON CONFLICT (code) DO UPDATE
SET base_url = EXCLUDED.base_url,
    api_key_env = EXCLUDED.api_key_env,
    default_model = EXCLUDED.default_model,
    is_enabled = true;

INSERT INTO public.kb_embedding_profile (
    id, provider_code, model, dimension, is_enabled
)
VALUES (
    'b1000001-0000-4000-8000-0000000000b1',
    'siliconflow',
    'BAAI/bge-m3',
    1024,
    true
)
ON CONFLICT (id) DO UPDATE
SET provider_code = EXCLUDED.provider_code,
    model = EXCLUDED.model,
    dimension = EXCLUDED.dimension,
    is_enabled = true,
    is_deleted = false;

INSERT INTO public.ai_global_config (config_key, config_value, description)
VALUES
    ('kb_handbook_max_top_distance', '0.45', '培训问答：最近一块距离超过该值则判定教材未覆盖'),
    ('kb_handbook_max_chunk_distance', '0.55', '培训问答：超过该距离的块不送入对话模型')
ON CONFLICT (config_key) DO NOTHING;

INSERT INTO public.ai_prompt_template (
    id, code, version, system_prompt, user_prompt_template, output_format, json_schema_hint, is_active
)
VALUES (
    'a2000001-0000-4000-8000-0000000000b1',
    'knowledge.handbook.qa',
    1,
    '你是电子元器件分销行业新人培训教材助教。只根据给定片段回答。数字、公式、阈值和话术必须与片段一致，不要补充公司制度或系统操作。片段不够时 covered 为 false，answer 为空字符串。只返回 JSON 对象，键为 covered 和 answer。',
    convert_from(decode('e997aee9a298efbc9a', 'hex'), 'UTF8')
        || CHR(123) || CHR(123) || 'question' || CHR(125) || CHR(125)
        || convert_from(decode('0ae69599e69d90e78987e6aeb5efbc9a0a', 'hex'), 'UTF8')
        || CHR(123) || CHR(123) || 'context' || CHR(125) || CHR(125)
        || convert_from(decode('0a', 'hex'), 'UTF8')
        || CHR(123) || CHR(123) || 'compliance_hint' || CHR(125) || CHR(125),
    'json',
    'covered: boolean; answer: string',
    true
)
ON CONFLICT (code, version) DO NOTHING;

INSERT INTO public.ai_scenario (
    id, code, name, description, provider_code, model, prompt_template_id,
    cache_ttl_seconds, cache_key_fields, allowed_input_fields, max_tokens, temperature,
    permission_code, rate_limit_per_user_per_min, is_enabled, enable_web_search
)
VALUES (
    'a3000001-0000-4000-8000-0000000000b1',
    'knowledge.handbook.qa',
    '培训教材问答',
    '按教材切块检索后回答，不编造教材外的公司制度',
    'moonshot',
    'kimi-k2.6',
    'a2000001-0000-4000-8000-0000000000b1',
    604800,
    jsonb_build_array('question', 'corpus_version_id', 'chunk_ids'),
    jsonb_build_array('question', 'corpus_version_id', 'chunk_ids', 'context', 'compliance_hint'),
    8192,
    0.20,
    'biz.ai.kb.qa',
    10,
    true,
    false
)
ON CONFLICT (code) DO NOTHING;

-- kimi-k2.6 的思考和正文共用 max_tokens。2048 会在教材问答上被思考占满，正文为空，页面显示「这次没有生成回答」。
UPDATE public.ai_scenario
SET max_tokens = 8192
WHERE code = 'knowledge.handbook.qa'
  AND max_tokens < 8192;

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000d1', 'biz.ai.kb.qa', 'AI-培训教材问答', 'api', 'kb', 'ask', 1, NOW()),
    ('30000000-0000-4000-8000-0000000000d2', 'biz.ai.kb.admin', 'AI-培训教材管理', 'api', 'kb', 'admin', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN ('biz.ai.kb.qa', 'biz.ai.kb.admin')
  AND p."Status" = 1
WHERE r."RoleCode" IN ('SYS_ADMIN', 'biz_all')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );
