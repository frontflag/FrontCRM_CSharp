using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 系统参数表 + 默认「显示时区」参数（IANA，默认 Asia/Shanghai）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260325120000_EnsureSysParamAndDisplayTimeZone")]
    public partial class EnsureSysParamAndDisplayTimeZone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.sysparamgroup (
    ""GroupId"" character varying(36) NOT NULL,
    ""GroupCode"" character varying(50) NOT NULL,
    ""GroupName"" character varying(100) NOT NULL,
    ""ParentId"" character varying(36) NULL,
    ""Level"" smallint NOT NULL DEFAULT 1,
    ""SortOrder"" integer NOT NULL DEFAULT 0,
    ""Description"" character varying(200) NULL,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_sysparamgroup"" PRIMARY KEY (""GroupId"")
);

CREATE TABLE IF NOT EXISTS public.sysparam (
    ""ParamId"" character varying(36) NOT NULL,
    ""ParamCode"" character varying(100) NOT NULL,
    ""ParamName"" character varying(200) NOT NULL,
    ""GroupId"" character varying(36) NULL,
    ""DataType"" smallint NOT NULL,
    ""ValueString"" character varying(500) NULL,
    ""ValueInt"" bigint NULL,
    ""ValueDecimal"" numeric(18,4) NULL,
    ""ValueJson"" text NULL,
    ""DefaultValue"" character varying(500) NULL,
    ""Description"" character varying(500) NULL,
    ""IsArray"" boolean NOT NULL DEFAULT FALSE,
    ""IsSystem"" boolean NOT NULL DEFAULT FALSE,
    ""IsEditable"" boolean NOT NULL DEFAULT TRUE,
    ""IsVisible"" boolean NOT NULL DEFAULT TRUE,
    ""SortOrder"" integer NOT NULL DEFAULT 0,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_sysparam"" PRIMARY KEY (""ParamId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_sysparam_ParamCode"" ON public.sysparam (""ParamCode"");

INSERT INTO public.sysparamgroup (""GroupId"", ""GroupCode"", ""GroupName"", ""ParentId"", ""Level"", ""SortOrder"", ""Description"", ""Status"", ""CreateTime"")
SELECT '00000000-0000-4000-8000-000000000001', 'System.Display', '显示与格式', NULL, 1, 0, '界面日期时间显示等', 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM public.sysparamgroup WHERE ""GroupId"" = '00000000-0000-4000-8000-000000000001');

INSERT INTO public.sysparam (""ParamId"", ""ParamCode"", ""ParamName"", ""GroupId"", ""DataType"", ""ValueString"", ""DefaultValue"", ""Description"", ""IsArray"", ""IsSystem"", ""IsEditable"", ""IsVisible"", ""SortOrder"", ""Status"", ""CreateTime"")
SELECT '00000000-0000-4000-8000-000000000002', 'System.Display.TimeZoneId', '前端显示用 IANA 时区', '00000000-0000-4000-8000-000000000001', 1, 'Asia/Shanghai', 'Asia/Shanghai', '例如 Asia/Shanghai、UTC。全站展示日期时间按此时区格式化。', FALSE, TRUE, TRUE, TRUE, 0, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM public.sysparam WHERE ""ParamCode"" = 'System.Display.TimeZoneId');
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
