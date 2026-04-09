-- 采购员无法「新建供应商」时排查：把下面三处 'YOUR_LOGIN' 换成实际登录名后执行。

-- 1) 角色 purchase_buyer 是否已绑定 vendor.write（INSERT 0 行通常表示这里本来就有）
SELECT 'purchase_buyer -> vendor.write' AS check_name,
       r."RoleCode", p."PermissionCode"
FROM sys_role r
JOIN sys_role_permission rp ON rp."RoleId" = r."RoleId"
JOIN sys_permission p ON p."PermissionId" = rp."PermissionId"
WHERE r."RoleCode" = 'purchase_buyer' AND p."PermissionCode" = 'vendor.write';

-- 2) 该用户绑定的角色（若无 purchase_buyer，仅靠 DEPT_* 时旧版无 vendor.write）
SELECT 'user roles' AS check_name, r."RoleCode", r."RoleName"
FROM "user" u
JOIN sys_user_role ur ON ur."UserId" = u."UserId"
JOIN sys_role r ON r."RoleId" = ur."RoleId"
WHERE u."UserName" = 'YOUR_LOGIN';

-- 3) 主部门须为采购部且 IdentityType=2（后端 RbacService 会对 identityType=2 合并 vendor.read/write）
SELECT 'departments' AS check_name,
       d."DepartmentName", d."IdentityType", ud."IsPrimary"
FROM "user" u
JOIN sys_user_department ud ON ud."UserId" = u."UserId"
JOIN sys_department d ON d."DepartmentId" = ud."DepartmentId"
WHERE u."UserName" = 'YOUR_LOGIN'
ORDER BY ud."IsPrimary" DESC NULLS LAST;

-- 4) 仅从角色解析出的 vendor.*（与登录 JWT 里旧逻辑一致；不含部门补丁）
SELECT DISTINCT p."PermissionCode"
FROM "user" u
JOIN sys_user_role ur ON ur."UserId" = u."UserId"
JOIN sys_role_permission rp ON rp."RoleId" = ur."RoleId"
JOIN sys_permission p ON p."PermissionId" = rp."PermissionId"
WHERE u."UserName" = 'YOUR_LOGIN'
  AND p."PermissionCode" LIKE 'vendor.%'
  AND p."Status" = 1;
