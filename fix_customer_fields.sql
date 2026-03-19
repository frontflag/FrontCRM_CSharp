-- 修复 customerinfo 表缺失字段
-- 执行时间: 2026-03-18

-- 添加黑名单相关字段
ALTER TABLE customerinfo 
    ADD COLUMN IF NOT EXISTS "BlackListAt" timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "BlackListByUserId" character varying(36),
    ADD COLUMN IF NOT EXISTS "BlackListByUserName" character varying(64),
    ADD COLUMN IF NOT EXISTS "BlackListReason" character varying(500);

-- 添加软删除相关字段
ALTER TABLE customerinfo 
    ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "DeletedByUserId" character varying(36),
    ADD COLUMN IF NOT EXISTS "DeletedByUserName" character varying(64),
    ADD COLUMN IF NOT EXISTS "DeleteReason" character varying(500);

-- 验证字段是否添加成功
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'customerinfo'
ORDER BY ordinal_position;
