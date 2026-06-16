-- biz_brand 批量导入前：移除表上除主键外的所有唯一索引/唯一约束
-- 说明：当前建表脚本仅含普通索引；若你曾手工加过唯一约束，本脚本会一并删除。
-- 导入完成后请执行 biz_brand_enable_unique_after_import_postgresql.sql（去重后）

DO $$
DECLARE
    r RECORD;
BEGIN
    -- 唯一约束（CONSTRAINT … UNIQUE）
    FOR r IN
        SELECT c.conname
        FROM pg_constraint c
        JOIN pg_class t ON t.oid = c.conrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'public'
          AND t.relname = 'biz_brand'
          AND c.contype = 'u'
    LOOP
        EXECUTE format('ALTER TABLE public.biz_brand DROP CONSTRAINT IF EXISTS %I', r.conname);
        RAISE NOTICE 'Dropped unique constraint: %', r.conname;
    END LOOP;

    -- 唯一索引（含 CREATE UNIQUE INDEX，不含主键）
    FOR r IN
        SELECT i.relname AS index_name
        FROM pg_index ix
        JOIN pg_class t ON t.oid = ix.indrelid
        JOIN pg_class i ON i.oid = ix.indexrelid
        JOIN pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname = 'public'
          AND t.relname = 'biz_brand'
          AND ix.indisunique
          AND NOT ix.indisprimary
    LOOP
        EXECUTE format('DROP INDEX IF EXISTS public.%I', r.index_name);
        RAISE NOTICE 'Dropped unique index: %', r.index_name;
    END LOOP;
END $$;

-- 验证：应只剩主键 biz_brand_pkey + 普通索引 IX_biz_brand_*
-- SELECT indexname, indexdef FROM pg_indexes WHERE schemaname = 'public' AND tablename = 'biz_brand' ORDER BY indexname;
