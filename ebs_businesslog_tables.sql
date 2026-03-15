-- ============================================================
-- 业务操作日志表结构
-- 记录所有业务动作的操作日志
-- ============================================================

-- 1. 业务操作日志主表
DROP TABLE IF EXISTS "businesslogdetail" CASCADE;
DROP TABLE IF EXISTS "businesslog" CASCADE;

CREATE TABLE IF NOT EXISTS "businesslog" (
    "LogId" char(36) NOT NULL,
    "BusinessModule" smallint NOT NULL,
    "ActionType" smallint NOT NULL,
    "DocumentType" varchar(50) NOT NULL,
    "BusinessDataId" char(36) NOT NULL,
    "DocumentCode" varchar(50),
    "OperatorId" char(36) NOT NULL,
    "OperatorName" varchar(50),
    "OperationTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "IpAddress" varchar(50),
    "DeviceInfo" varchar(200),
    "OperationResult" boolean DEFAULT true,
    "ResultMessage" varchar(200),
    "OperationDescription" varchar(500),
    "OperationSummary" varchar(1000),
    "RelatedDocumentId" char(36),
    "RelatedDocumentType" varchar(50),
    "ApprovalFlowId" char(36),
    "ApprovalNodeId" char(36),
    "DataSource" smallint DEFAULT 1,
    "TenantId" char(36),
    "Remark" varchar(500),
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    "ModifyTime" timestamp,
    "ModifyUserId" bigint,
    PRIMARY KEY ("LogId")
);

-- 2. 业务操作日志明细表
CREATE TABLE IF NOT EXISTS "businesslogdetail" (
    "DetailId" char(36) NOT NULL,
    "LogId" char(36) NOT NULL,
    "FieldName" varchar(50) NOT NULL,
    "FieldDescription" varchar(100),
    "OldValue" varchar(1000),
    "NewValue" varchar(1000),
    "ChangeType" smallint DEFAULT 2,
    "DataType" varchar(20),
    "IsKeyField" boolean DEFAULT false,
    "CreateTime" timestamp DEFAULT CURRENT_TIMESTAMP,
    "CreateUserId" bigint,
    PRIMARY KEY ("DetailId"),
    CONSTRAINT "FK_BusinessLogDetail_BusinessLog" 
        FOREIGN KEY ("LogId") REFERENCES "businesslog"("LogId") ON DELETE CASCADE
);

-- 3. 创建索引
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_Module" ON "businesslog" ("BusinessModule");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_ActionType" ON "businesslog" ("ActionType");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_DocumentType" ON "businesslog" ("DocumentType");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_BusinessDataId" ON "businesslog" ("BusinessDataId");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_DocumentCode" ON "businesslog" ("DocumentCode");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_OperatorId" ON "businesslog" ("OperatorId");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_OperationTime" ON "businesslog" ("OperationTime");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_RelatedDoc" ON "businesslog" ("RelatedDocumentId", "RelatedDocumentType");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLog_Tenant" ON "businesslog" ("TenantId");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLogDetail_LogId" ON "businesslogdetail" ("LogId");
CREATE INDEX IF NOT EXISTS "IDX_BusinessLogDetail_FieldName" ON "businesslogdetail" ("FieldName");

-- 4. 添加表注释
COMMENT ON TABLE "businesslog" IS '业务操作日志主表';
COMMENT ON TABLE "businesslogdetail" IS '业务操作日志明细表';

-- 5. 验证表创建
SELECT '业务日志表创建完成!' AS message;

SELECT 
    table_name,
    '已创建' AS status
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('businesslog', 'businesslogdetail')
ORDER BY table_name;
