-- 部门表：财务数据范围 + 只读/读写（与迁移 20260526160000_DepartmentFinanceDataScope 一致）

BEGIN;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "FinanceDataScope" smallint NOT NULL DEFAULT 0;

ALTER TABLE public.sys_department
  ADD COLUMN IF NOT EXISTS "FinanceDataAccess" smallint NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.sys_department."FinanceDataScope" IS '财务数据权限：0全部 1自己 2本部门 3本部门及下级 4禁止';
COMMENT ON COLUMN public.sys_department."FinanceDataAccess" IS '财务数据访问：0读写 1只读（与 FinanceDataScope 独立）';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260526160000_DepartmentFinanceDataScope', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;
