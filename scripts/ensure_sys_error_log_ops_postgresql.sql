-- 运维「系统错误」权限（幂等）
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT gen_random_uuid()::text, 'sys.errorlog.read', '系统错误-查看', 'api', 'errorlog', 'read', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'sys.errorlog.read');

INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
SELECT gen_random_uuid()::text, 'sys.errorlog.resolve', '系统错误-标记处理', 'api', 'errorlog', 'resolve', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM sys_permission WHERE "PermissionCode" = 'sys.errorlog.resolve');

INSERT INTO sys_role_permission ("RolePermissionId", "RoleId", "PermissionId", "CreateTime")
SELECT gen_random_uuid()::text, r."RoleId", p."PermissionId", NOW()
FROM sys_role r
JOIN sys_permission p ON p."PermissionCode" IN ('sys.errorlog.read', 'sys.errorlog.resolve') AND p."Status" = 1
WHERE r."RoleCode" = 'SYS_ADMIN'
  AND NOT EXISTS (
    SELECT 1 FROM sys_role_permission x
    WHERE x."RoleId" = r."RoleId" AND x."PermissionId" = p."PermissionId"
  );
