using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 全系统操作日志统一至 log_operation，字段变更统一至 log_change_fldval；
    /// 迁移旧表 customer_* / vendor_* 数据后删除旧表。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260328210000_UnifiedLogOperationAndChangeFldval")]
    public partial class UnifiedLogOperationAndChangeFldval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.log_operation (
    ""Id"" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    ""BizType"" VARCHAR(64) NOT NULL,
    ""RecordId"" TEXT NOT NULL,
    ""RecordCode"" VARCHAR(128),
    ""ActionType"" VARCHAR(100) NOT NULL,
    ""OperationTime"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ""OperatorUserId"" TEXT,
    ""OperatorUserName"" VARCHAR(100),
    ""Reason"" TEXT,
    ""ExtraInfo"" TEXT,
    ""SysRemark"" TEXT,
    ""OperationDesc"" TEXT
);
CREATE INDEX IF NOT EXISTS idx_log_operation_biz_record ON public.log_operation(""BizType"", ""RecordId"");
CREATE INDEX IF NOT EXISTS idx_log_operation_time ON public.log_operation(""OperationTime"");

CREATE TABLE IF NOT EXISTS public.log_change_fldval (
    ""Id"" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    ""BizType"" VARCHAR(64) NOT NULL,
    ""RecordId"" TEXT NOT NULL,
    ""RecordCode"" VARCHAR(128),
    ""FieldName"" VARCHAR(100) NOT NULL,
    ""FieldLabel"" VARCHAR(200),
    ""OldValue"" TEXT,
    ""NewValue"" TEXT,
    ""ChangedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ""ChangedByUserId"" TEXT,
    ""ChangedByUserName"" VARCHAR(100),
    ""ExtraInfo"" TEXT,
    ""SysRemark"" TEXT
);
CREATE INDEX IF NOT EXISTS idx_log_change_fldval_biz_record ON public.log_change_fldval(""BizType"", ""RecordId"");
CREATE INDEX IF NOT EXISTS idx_log_change_fldval_changed_at ON public.log_change_fldval(""ChangedAt"");
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'customer_operation_log') THEN
    INSERT INTO public.log_operation (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""ActionType"", ""OperationTime"", ""OperatorUserId"", ""OperatorUserName"", ""Reason"", ""ExtraInfo"", ""SysRemark"", ""OperationDesc"")
    SELECT col.""Id"", 'Customer', col.""CustomerId"", c.""CustomerCode"", col.""OperationType"", col.""OperationTime"", col.""OperatorUserId"", col.""OperatorUserName"", col.""Remark"", NULL, NULL, col.""OperationDesc""
    FROM public.customer_operation_log col
    LEFT JOIN public.customerinfo c ON c.""CustomerId"" = col.""CustomerId""
    WHERE NOT EXISTS (SELECT 1 FROM public.log_operation o WHERE o.""Id"" = col.""Id"");
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'customer_change_log') THEN
    INSERT INTO public.log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
    SELECT ch.""Id"", 'Customer', ch.""CustomerId"", c.""CustomerCode"", ch.""FieldName"", ch.""FieldLabel"", ch.""OldValue"", ch.""NewValue"", ch.""ChangedAt"", ch.""ChangedByUserId"", ch.""ChangedByUserName"", NULL, NULL
    FROM public.customer_change_log ch
    LEFT JOIN public.customerinfo c ON c.""CustomerId"" = ch.""CustomerId""
    WHERE NOT EXISTS (SELECT 1 FROM public.log_change_fldval x WHERE x.""Id"" = ch.""Id"");
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'vendor_operation_log') THEN
    INSERT INTO public.log_operation (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""ActionType"", ""OperationTime"", ""OperatorUserId"", ""OperatorUserName"", ""Reason"", ""ExtraInfo"", ""SysRemark"", ""OperationDesc"")
    SELECT v.""Id"", 'Vendor', v.""VendorId"", vi.""Code"", v.""OperationType"", v.""OperationTime"", v.""OperatorUserId"", v.""OperatorUserName"", v.""Remark"", NULL, NULL, v.""OperationDesc""
    FROM public.vendor_operation_log v
    LEFT JOIN public.vendorinfo vi ON vi.""VendorId"" = v.""VendorId""
    WHERE NOT EXISTS (SELECT 1 FROM public.log_operation o WHERE o.""Id"" = v.""Id"");
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'vendor_change_log') THEN
    INSERT INTO public.log_change_fldval (""Id"", ""BizType"", ""RecordId"", ""RecordCode"", ""FieldName"", ""FieldLabel"", ""OldValue"", ""NewValue"", ""ChangedAt"", ""ChangedByUserId"", ""ChangedByUserName"", ""ExtraInfo"", ""SysRemark"")
    SELECT ch.""Id"", 'Vendor', ch.""VendorId"", vi.""Code"", ch.""FieldName"", ch.""FieldLabel"", ch.""OldValue"", ch.""NewValue"", ch.""ChangedAt"", ch.""ChangedByUserId"", ch.""ChangedByUserName"", NULL, NULL
    FROM public.vendor_change_log ch
    LEFT JOIN public.vendorinfo vi ON vi.""VendorId"" = ch.""VendorId""
    WHERE NOT EXISTS (SELECT 1 FROM public.log_change_fldval x WHERE x.""Id"" = ch.""Id"");
  END IF;
END $$;
");

            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS public.customer_operation_log;
DROP TABLE IF EXISTS public.customer_change_log;
DROP TABLE IF EXISTS public.vendor_operation_log;
DROP TABLE IF EXISTS public.vendor_change_log;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 不回滚：避免误删已写入的统一日志；如需回滚请从备份恢复。
        }
    }
}
