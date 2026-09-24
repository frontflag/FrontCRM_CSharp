-- 增量：知识库问答建表（pgvector）
-- DBeaver-safe：本脚本只有 DDL，无双花括号占位符
-- 依赖：PostgreSQL 已安装 pgvector，可执行 CREATE EXTENSION vector

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
