-- 更新部门和角色数据
-- 部门: 业务部, 采购部, 物流部, 财务部
-- 角色: 每个部门设置 经理, 员工

-- 1. 清空并插入部门数据
DELETE FROM department;

INSERT INTO "department" ("DepartmentId", "DepartmentCode", "DepartmentName", "ParentId", "ManagerId", "Level", "SortOrder", "Status", "CreateTime") VALUES
('d1', 'SALE', 'Business Dept', NULL, NULL, 1, 1, 1, CURRENT_TIMESTAMP),
('d2', 'PUR', 'Purchase Dept', NULL, NULL, 1, 2, 1, CURRENT_TIMESTAMP),
('d3', 'LOG', 'Logistics Dept', NULL, NULL, 1, 3, 1, CURRENT_TIMESTAMP),
('d4', 'FIN', 'Finance Dept', NULL, NULL, 1, 4, 1, CURRENT_TIMESTAMP);

-- 2. 清空并插入角色数据
DELETE FROM "role";

INSERT INTO "role" ("RoleId", "RoleCode", "RoleName", "RoleType", "Description", "SortOrder", "Status", "IsSystem", "CreateTime") VALUES
('r0', 'admin', 'System Admin', 1, 'Full system access', 0, 1, true, CURRENT_TIMESTAMP),
('r1', 'sale_mgr', 'Sales Manager', 2, 'Business dept manager', 1, 1, false, CURRENT_TIMESTAMP),
('r2', 'sale_staff', 'Sales Staff', 2, 'Business dept staff', 2, 1, false, CURRENT_TIMESTAMP),
('r3', 'pur_mgr', 'Purchase Manager', 2, 'Purchase dept manager', 3, 1, false, CURRENT_TIMESTAMP),
('r4', 'pur_staff', 'Purchase Staff', 2, 'Purchase dept staff', 4, 1, false, CURRENT_TIMESTAMP),
('r5', 'log_mgr', 'Logistics Manager', 2, 'Logistics dept manager', 5, 1, false, CURRENT_TIMESTAMP),
('r6', 'log_staff', 'Logistics Staff', 2, 'Logistics dept staff', 6, 1, false, CURRENT_TIMESTAMP),
('r7', 'fin_mgr', 'Finance Manager', 2, 'Finance dept manager', 7, 1, false, CURRENT_TIMESTAMP),
('r8', 'fin_staff', 'Finance Staff', 2, 'Finance dept staff', 8, 1, false, CURRENT_TIMESTAMP);

-- 3. 显示结果
SELECT 'DEPARTMENT' AS type, "DepartmentCode" AS code, "DepartmentName" AS name FROM department
UNION ALL
SELECT 'ROLE' AS type, "RoleCode" AS code, "RoleName" AS name FROM "role"
ORDER BY type, code;
