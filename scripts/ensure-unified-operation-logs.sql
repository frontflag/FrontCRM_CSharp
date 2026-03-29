-- 全系统操作日志 / 字段变更日志（与 EF 迁移 20260328210000_UnifiedLogOperationAndChangeFldval 一致）
-- 若已执行 dotnet ef database update，无需再手工执行本脚本。

CREATE TABLE IF NOT EXISTS public.log_operation (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "BizType" VARCHAR(64) NOT NULL,
    "RecordId" TEXT NOT NULL,
    "RecordCode" VARCHAR(128),
    "ActionType" VARCHAR(100) NOT NULL,
    "OperationTime" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "OperatorUserId" TEXT,
    "OperatorUserName" VARCHAR(100),
    "Reason" TEXT,
    "ExtraInfo" TEXT,
    "SysRemark" TEXT,
    "OperationDesc" TEXT
);
CREATE INDEX IF NOT EXISTS idx_log_operation_biz_record ON public.log_operation("BizType", "RecordId");
CREATE INDEX IF NOT EXISTS idx_log_operation_time ON public.log_operation("OperationTime");

CREATE TABLE IF NOT EXISTS public.log_change_fldval (
    "Id" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    "BizType" VARCHAR(64) NOT NULL,
    "RecordId" TEXT NOT NULL,
    "RecordCode" VARCHAR(128),
    "FieldName" VARCHAR(100) NOT NULL,
    "FieldLabel" VARCHAR(200),
    "OldValue" TEXT,
    "NewValue" TEXT,
    "ChangedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "ChangedByUserId" TEXT,
    "ChangedByUserName" VARCHAR(100),
    "ExtraInfo" TEXT,
    "SysRemark" TEXT
);
CREATE INDEX IF NOT EXISTS idx_log_change_fldval_biz_record ON public.log_change_fldval("BizType", "RecordId");
CREATE INDEX IF NOT EXISTS idx_log_change_fldval_changed_at ON public.log_change_fldval("ChangedAt");
