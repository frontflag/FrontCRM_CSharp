-- ============================================================
-- 更新部门和角色数据
-- 部门: 业务部, 采购部, 物流部, 财务部
-- 角色: 每个部门设置 经理, 员工
-- ============================================================

-- 1. 清空现有部门数据并重新插入
DELETE FROM department;

INSERT INTO "department" ("DepartmentId", "DepartmentCode", "DepartmentName", "ParentId", "ManagerId", "Level", "SortOrder", "Status", "CreateTime") VALUES
('d1', 'SALE', '业务部', NULL, NULL, 1, 1, 1, CURRENT_TIMESTAMP),
('d2', 'PUR', '采购部', NULL, NULL, 1, 2, 1, CURRENT_TIMESTAMP),
('d3', 'LOG', '物流部', NULL, NULL, 1, 3, 1, CURRENT_TIMESTAMP),
('d4', 'FIN', '财务部', NULL, NULL, 1, 4, 1, CURRENT_TIMESTAMP);

-- 2. 清空现有角色数据并重新插入
DELETE FROM "role";

INSERT INTO "role" ("RoleId", "RoleCode", "RoleName", "RoleType", "Description", "SortOrder", "Status", "IsSystem", "CreateTime") VALUES
-- 系统管理员
('r0', 'admin', '系统管理员', 1, '系统超级管理员，拥有所有权限', 0, 1, true, CURRENT_TIMESTAMP),
-- 业务部
('r1', 'sale_manager', '业务部经理', 2, '业务部部门经理', 1, 1, false, CURRENT_TIMESTAMP),
('r2', 'sale_staff', '业务部员工', 2, '业务部普通员工', 2, 1, false, CURRENT_TIMESTAMP),
-- 采购部
('r3', 'pur_manager', '采购部经理', 2, '采购部部门经理', 3, 1, false, CURRENT_TIMESTAMP),
('r4', 'pur_staff', '采购部员工', 2, '采购部普通员工', 4, 1, false, CURRENT_TIMESTAMP),
-- 物流部
('r5', 'log_manager', '物流部经理', 2, '物流部部门经理', 5, 1, false, CURRENT_TIMESTAMP),
('r6', 'log_staff', '物流部员工', 2, '物流部普通员工', 6, 1, false, CURRENT_TIMESTAMP),
-- 财务部
('r7', 'fin_manager', '财务部经理', 2, '财务部部门经理', 7, 1, false, CURRENT_TIMESTAMP),
('r8', 'fin_staff', '财务部员工', 2, '财务部普通员工', 8, 1, false, CURRENT_TIMESTAMP);

-- 3. 验证数据
SELECT '部门数据' AS category, "DepartmentCode" AS code, "DepartmentName" AS name FROM department
UNION ALL
SELECT '角色数据' AS category, "RoleCode" AS code, "RoleName" AS name FROM "role"
ORDER BY category, code;
