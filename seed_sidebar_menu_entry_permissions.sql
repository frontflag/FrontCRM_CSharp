-- 侧栏「主菜单入口」对应权限（幂等）
-- 补齐生产库可能缺失的：销售/采购分析、品牌管理等，供角色编辑勾选。
INSERT INTO sys_permission ("PermissionId", "PermissionCode", "PermissionName", "PermissionType", "Resource", "Action", "Status", "CreateTime")
VALUES
  ('a0000000-0000-4000-8000-000000000101', 'analytics-sales.read', '销售分析-查看', 'api', 'analytics', 'read', 1, NOW()),
  ('a0000000-0000-4000-8000-000000000102', 'analytics-purchase.read', '采购分析-查看', 'api', 'analytics', 'read', 1, NOW()),
  ('a0000000-0000-4000-8000-000000000103', 'analytics-logistics.read', '物流分析-查看', 'api', 'analytics', 'read', 1, NOW()),
  ('a0000000-0000-4000-8000-000000000104', 'analytics-finance.read', '财务分析-查看', 'api', 'analytics', 'read', 1, NOW()),
  ('30000000-0000-4000-8000-0000000000b0', 'biz-brand.read', '品牌管理-查看', 'api', 'biz-brand', 'read', 1, NOW()),
  ('30000000-0000-4000-8000-0000000000b1', 'biz-brand.write', '品牌管理-维护', 'api', 'biz-brand', 'write', 1, NOW()),
  ('30000000-0000-4000-8000-0000000000c1', 'biz.feedback.admin', '用户反馈-运维', 'api', 'feedback', 'admin', 1, NOW()),
  ('30000000-0000-4000-8000-0000000000c2', 'sys.errorlog.read', '系统错误-查看', 'api', 'errorlog', 'read', 1, NOW()),
  ('30000000-0000-4000-8000-0000000000c3', 'biz.telemetry.analytics', '埋点分析-查看', 'api', 'telemetry', 'analytics', 1, NOW())
ON CONFLICT ("PermissionCode") DO UPDATE
SET
  "PermissionName" = EXCLUDED."PermissionName",
  "PermissionType" = EXCLUDED."PermissionType",
  "Resource" = EXCLUDED."Resource",
  "Action" = EXCLUDED."Action",
  "Status" = 1;

-- 核对主菜单入口相关权限是否齐全
SELECT "PermissionCode", "PermissionName", "Status"
FROM sys_permission
WHERE "PermissionCode" IN (
  'analytics-sales.read',
  'analytics-purchase.read',
  'analytics-logistics.read',
  'analytics-finance.read',
  'biz-brand.read',
  'biz.feedback.admin',
  'sys.errorlog.read',
  'biz.telemetry.analytics'
)
ORDER BY "PermissionCode";
