-- ============================================================
-- 用户、角色、权限管理表结构
-- ============================================================

SET search_path TO public;

-- 1. 用户表
DROP TABLE IF EXISTS "userrole" CASCADE;
DROP TABLE IF EXISTS "user" CASCADE;

CREATE TABLE IF NOT EXISTS "user" (
    "UserId" char(36) NOT NULL,
    "UserName" varchar(50) NOT NULL,
    "Password" varchar(256) NOT NULL,
    "Salt" varchar(50) NOT NULL,
    "RealName" varchar(50),
    "NickName" varchar(50),
    "Email" varchar(100),
    "Phone" varchar(20),
    "Mobile" varchar(20),
    "Avatar" varchar(500),
    "DepartmentId" char(36),
    "DepartmentName" varchar(100),
    "Position" varchar(50),
    "EmployeeNo" varchar(20),
    "Gender" smallint DEFAULT 0,
    "Birthday" date,
    "IdCard" varchar(18),
    "Address" varchar(200),
    "Status" smallint DEFAULT 1,
    "IsAdmin" boolean DEFAULT false,
    "IsDeleted" boolean DEFAULT false,
    "LastLoginTime" timestamp,
    "LastLoginIp" varchar(50),
    "LoginCount" integer DEFAULT 0,
    "PasswordChangeTime" timestamp,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("UserId"),
    CONSTRAINT "UK_UserName" UNIQUE ("UserName"),
    CONSTRAINT "UK_Email" UNIQUE ("Email")
);

-- 2. 角色表
DROP TABLE IF EXISTS "rolepermission" CASCADE;
DROP TABLE IF EXISTS "role" CASCADE;

CREATE TABLE IF NOT EXISTS "role" (
    "RoleId" char(36) NOT NULL,
    "RoleName" varchar(50) NOT NULL,
    "RoleCode" varchar(50) NOT NULL,
    "RoleType" smallint DEFAULT 1,
    "Description" varchar(200),
    "SortOrder" integer DEFAULT 0,
    "Status" smallint DEFAULT 1,
    "IsSystem" boolean DEFAULT false,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("RoleId"),
    CONSTRAINT "UK_RoleCode" UNIQUE ("RoleCode")
);

-- 3. 权限表
DROP TABLE IF EXISTS "permission" CASCADE;

CREATE TABLE IF NOT EXISTS "permission" (
    "PermissionId" char(36) NOT NULL,
    "PermissionName" varchar(50) NOT NULL,
    "PermissionCode" varchar(100) NOT NULL,
    "ParentId" char(36),
    "PermissionType" smallint DEFAULT 1,
    "Icon" varchar(50),
    "Path" varchar(200),
    "Component" varchar(200),
    "SortOrder" integer DEFAULT 0,
    "Status" smallint DEFAULT 1,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("PermissionId"),
    CONSTRAINT "UK_PermissionCode" UNIQUE ("PermissionCode")
);

-- 4. 用户角色关联表
CREATE TABLE IF NOT EXISTS "userrole" (
    "Id" char(36) NOT NULL,
    "UserId" char(36) NOT NULL,
    "RoleId" char(36) NOT NULL,
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("Id"),
    CONSTRAINT "UK_UserRole" UNIQUE ("UserId", "RoleId"),
    CONSTRAINT "FK_UserRole_User" FOREIGN KEY ("UserId") REFERENCES "user"("UserId") ON DELETE CASCADE,
    CONSTRAINT "FK_UserRole_Role" FOREIGN KEY ("RoleId") REFERENCES "role"("RoleId") ON DELETE CASCADE
);

-- 5. 角色权限关联表
CREATE TABLE IF NOT EXISTS "rolepermission" (
    "Id" char(36) NOT NULL,
    "RoleId" char(36) NOT NULL,
    "PermissionId" char(36) NOT NULL,
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("Id"),
    CONSTRAINT "UK_RolePermission" UNIQUE ("RoleId", "PermissionId"),
    CONSTRAINT "FK_RolePermission_Role" FOREIGN KEY ("RoleId") REFERENCES "role"("RoleId") ON DELETE CASCADE,
    CONSTRAINT "FK_RolePermission_Permission" FOREIGN KEY ("PermissionId") REFERENCES "permission"("PermissionId") ON DELETE CASCADE
);

-- 6. 部门表
DROP TABLE IF EXISTS "department" CASCADE;

CREATE TABLE IF NOT EXISTS "department" (
    "DepartmentId" char(36) NOT NULL,
    "DepartmentName" varchar(100) NOT NULL,
    "DepartmentCode" varchar(50),
    "ParentId" char(36),
    "ManagerId" char(36),
    "ManagerName" varchar(50),
    "Level" smallint DEFAULT 1,
    "SortOrder" integer DEFAULT 0,
    "Status" smallint DEFAULT 1,
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("DepartmentId"),
    CONSTRAINT "FK_Department_Parent" FOREIGN KEY ("ParentId") REFERENCES "department"("DepartmentId")
);

-- 7. 创建索引
CREATE INDEX IF NOT EXISTS "IDX_User_Department" ON "user" ("DepartmentId");
CREATE INDEX IF NOT EXISTS "IDX_User_Status" ON "user" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_Role_Type" ON "role" ("RoleType");
CREATE INDEX IF NOT EXISTS "IDX_Role_Status" ON "role" ("Status");
CREATE INDEX IF NOT EXISTS "IDX_Permission_Parent" ON "permission" ("ParentId");
CREATE INDEX IF NOT EXISTS "IDX_Permission_Type" ON "permission" ("PermissionType");
CREATE INDEX IF NOT EXISTS "IDX_UserRole_User" ON "userrole" ("UserId");
CREATE INDEX IF NOT EXISTS "IDX_UserRole_Role" ON "userrole" ("RoleId");
CREATE INDEX IF NOT EXISTS "IDX_RolePermission_Role" ON "rolepermission" ("RoleId");
CREATE INDEX IF NOT EXISTS "IDX_RolePermission_Permission" ON "rolepermission" ("PermissionId");
CREATE INDEX IF NOT EXISTS "IDX_Department_Parent" ON "department" ("ParentId");

-- 8. 插入默认数据

-- 默认角色
INSERT INTO "role" ("RoleId", "RoleName", "RoleCode", "RoleType", "Description", "IsSystem", "Status")
VALUES 
    ('00000000-0000-0000-0000-000000000001', '系统管理员', 'admin', 1, '系统超级管理员', true, 1),
    ('00000000-0000-0000-0000-000000000002', '销售经理', 'sales_manager', 2, '销售部门经理', false, 1),
    ('00000000-0000-0000-0000-000000000003', '销售员', 'sales', 2, '销售人员', false, 1),
    ('00000000-0000-0000-0000-000000000004', '采购经理', 'purchase_manager', 2, '采购部门经理', false, 1),
    ('00000000-0000-0000-0000-000000000005', '采购员', 'purchase', 2, '采购人员', false, 1),
    ('00000000-0000-0000-0000-000000000006', '财务人员', 'finance', 2, '财务人员', false, 1),
    ('00000000-0000-0000-0000-000000000007', '仓库管理员', 'warehouse', 2, '仓库管理人员', false, 1)
ON CONFLICT ("RoleId") DO NOTHING;

-- 默认权限（菜单）
INSERT INTO "permission" ("PermissionId", "PermissionName", "PermissionCode", "ParentId", "PermissionType", "Path", "Component", "SortOrder", "Status")
VALUES 
    -- 系统管理
    ('10000000-0000-0000-0000-000000000001', '系统管理', 'system', null, 1, '/system', 'Layout', 1, 1),
    ('10000000-0000-0000-0000-000000000002', '用户管理', 'system:user', '10000000-0000-0000-0000-000000000001', 1, 'user', 'system/user/index', 1, 1),
    ('10000000-0000-0000-0000-000000000003', '角色管理', 'system:role', '10000000-0000-0000-0000-000000000001', 1, 'role', 'system/role/index', 2, 1),
    ('10000000-0000-0000-0000-000000000004', '权限管理', 'system:permission', '10000000-0000-0000-0000-000000000001', 1, 'permission', 'system/permission/index', 3, 1),
    ('10000000-0000-0000-0000-000000000005', '部门管理', 'system:department', '10000000-0000-0000-0000-000000000001', 1, 'department', 'system/department/index', 4, 1),
    ('10000000-0000-0000-0000-000000000006', '日志管理', 'system:log', '10000000-0000-0000-0000-000000000001', 1, 'log', 'system/log/index', 5, 1),
    
    -- 客户管理
    ('20000000-0000-0000-0000-000000000001', '客户管理', 'customer', null, 1, '/customer', 'Layout', 2, 1),
    ('20000000-0000-0000-0000-000000000002', '客户列表', 'customer:list', '20000000-0000-0000-0000-000000000001', 1, 'list', 'customer/list/index', 1, 1),
    ('20000000-0000-0000-0000-000000000003', '联系人管理', 'customer:contact', '20000000-0000-0000-0000-000000000001', 1, 'contact', 'customer/contact/index', 2, 1),
    
    -- 供应商管理
    ('30000000-0000-0000-0000-000000000001', '供应商管理', 'vendor', null, 1, '/vendor', 'Layout', 3, 1),
    ('30000000-0000-0000-0000-000000000002', '供应商列表', 'vendor:list', '30000000-0000-0000-0000-000000000001', 1, 'list', 'vendor/list/index', 1, 1),
    
    -- 询价管理
    ('40000000-0000-0000-0000-000000000001', '询价管理', 'rfq', null, 1, '/rfq', 'Layout', 4, 1),
    ('40000000-0000-0000-0000-000000000002', '询价单', 'rfq:list', '40000000-0000-0000-0000-000000000001', 1, 'list', 'rfq/list/index', 1, 1),
    
    -- 报价管理
    ('50000000-0000-0000-0000-000000000001', '报价管理', 'quote', null, 1, '/quote', 'Layout', 5, 1),
    ('50000000-0000-0000-0000-000000000002', '报价单', 'quote:list', '50000000-0000-0000-0000-000000000001', 1, 'list', 'quote/list/index', 1, 1),
    
    -- 销售管理
    ('60000000-0000-0000-0000-000000000001', '销售管理', 'sales', null, 1, '/sales', 'Layout', 6, 1),
    ('60000000-0000-0000-0000-000000000002', '销售订单', 'sales:order', '60000000-0000-0000-0000-000000000001', 1, 'order', 'sales/order/index', 1, 1),
    
    -- 采购管理
    ('70000000-0000-0000-0000-000000000001', '采购管理', 'purchase', null, 1, '/purchase', 'Layout', 7, 1),
    ('70000000-0000-0000-0000-000000000002', '采购订单', 'purchase:order', '70000000-0000-0000-0000-000000000001', 1, 'order', 'purchase/order/index', 1, 1),
    
    -- 库存管理
    ('80000000-0000-0000-0000-000000000001', '库存管理', 'inventory', null, 1, '/inventory', 'Layout', 8, 1),
    ('80000000-0000-0000-0000-000000000002', '库存查询', 'inventory:stock', '80000000-0000-0000-0000-000000000001', 1, 'stock', 'inventory/stock/index', 1, 1),
    ('80000000-0000-0000-0000-000000000003', '入库管理', 'inventory:in', '80000000-0000-0000-0000-000000000001', 1, 'in', 'inventory/in/index', 2, 1),
    ('80000000-0000-0000-0000-000000000004', '出库管理', 'inventory:out', '80000000-0000-0000-0000-000000000001', 1, 'out', 'inventory/out/index', 3, 1),
    
    -- 财务管理
    ('90000000-0000-0000-0000-000000000001', '财务管理', 'finance', null, 1, '/finance', 'Layout', 9, 1),
    ('90000000-0000-0000-0000-000000000002', '发票管理', 'finance:invoice', '90000000-0000-0000-0000-000000000001', 1, 'invoice', 'finance/invoice/index', 1, 1),
    ('90000000-0000-0000-0000-000000000003', '付款管理', 'finance:payment', '90000000-0000-0000-0000-000000000001', 1, 'payment', 'finance/payment/index', 2, 1),
    ('90000000-0000-0000-0000-000000000004', '收款管理', 'finance:receipt', '90000000-0000-0000-0000-000000000001', 1, 'receipt', 'finance/receipt/index', 3, 1)
ON CONFLICT ("PermissionId") DO NOTHING;

-- 9. 验证表创建
SELECT '用户角色权限表创建完成!' AS message;

SELECT 
    table_name,
    '已创建' AS status
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('user', 'role', 'permission', 'userrole', 'rolepermission', 'department')
ORDER BY table_name;

-- 统计
SELECT 
    COUNT(*) AS user_role_tables,
    '用户角色权限表' AS description
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('user', 'role', 'permission', 'userrole', 'rolepermission', 'department');