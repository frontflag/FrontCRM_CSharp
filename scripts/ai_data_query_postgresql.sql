-- AI 数据查询：只增加提问权限。查数走既有报表，不新增提示词、不缓存结果。

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
    ('30000000-0000-4000-8000-0000000000d4', 'biz.ai.data.query', 'AI-数据查询', 'api', 'ai', 'data-query', 1, NOW())
ON CONFLICT ("PermissionCode") DO NOTHING;

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" = 'biz.ai.data.query'
  AND p."Status" = 1
WHERE r."RoleCode" IN ('SYS_ADMIN', 'SYS_MANAGER', 'biz_all')
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );
