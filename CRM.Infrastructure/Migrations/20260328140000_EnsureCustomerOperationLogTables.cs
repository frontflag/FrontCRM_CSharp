using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// CustomerService 通过原生 SQL 读写操作/变更日志；部分环境未执行 apply_migrations 会报 42P01（表不存在）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260328140000_EnsureCustomerOperationLogTables")]
    public partial class EnsureCustomerOperationLogTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.customer_operation_log (
    ""Id"" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    ""CustomerId"" TEXT NOT NULL,
    ""OperationType"" VARCHAR(100) NOT NULL,
    ""OperationDesc"" TEXT,
    ""OperatorUserId"" TEXT,
    ""OperatorUserName"" VARCHAR(100),
    ""OperationTime"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ""Remark"" TEXT
);
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_customer_id ON public.customer_operation_log(""CustomerId"");
CREATE INDEX IF NOT EXISTS idx_customer_operation_log_operation_time ON public.customer_operation_log(""OperationTime"");

CREATE TABLE IF NOT EXISTS public.customer_change_log (
    ""Id"" TEXT PRIMARY KEY DEFAULT gen_random_uuid()::text,
    ""CustomerId"" TEXT NOT NULL,
    ""FieldName"" VARCHAR(100) NOT NULL,
    ""FieldLabel"" VARCHAR(200),
    ""OldValue"" TEXT,
    ""NewValue"" TEXT,
    ""ChangedByUserId"" TEXT,
    ""ChangedByUserName"" VARCHAR(100),
    ""ChangedAt"" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_customer_change_log_customer_id ON public.customer_change_log(""CustomerId"");
CREATE INDEX IF NOT EXISTS idx_customer_change_log_changed_at ON public.customer_change_log(""ChangedAt"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 不删表，避免误删生产数据
        }
    }
}
