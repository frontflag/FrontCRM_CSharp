using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>对象关系配置表 sys_relation_map。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260604120000_AddSysRelationMap")]
    public partial class AddSysRelationMap : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.sys_relation_map (
    id BIGSERIAL PRIMARY KEY,
    type smallint NOT NULL,
    obj_src character varying(64) NOT NULL,
    obj_dest character varying(64) NOT NULL,
    remark character varying(500) NULL,
    is_deleted boolean NOT NULL DEFAULT false
);

COMMENT ON TABLE public.sys_relation_map IS '对象关系配置：type 定义关系语义，obj_src 配对 obj_dest';
COMMENT ON COLUMN public.sys_relation_map.type IS '关系类型：100-199 人员关系，200-299 业务关系（见 SysRelationMapTypeCode）';
COMMENT ON COLUMN public.sys_relation_map.obj_src IS '源对象标识（用户/业务主键等）';
COMMENT ON COLUMN public.sys_relation_map.obj_dest IS '目标对象标识';
COMMENT ON COLUMN public.sys_relation_map.is_deleted IS '软删除';

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_sys_relation_map_type_src_dest""
    ON public.sys_relation_map (type, obj_src, obj_dest)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_sys_relation_map_type_src""
    ON public.sys_relation_map (type, obj_src)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS ""IX_sys_relation_map_type_dest""
    ON public.sys_relation_map (type, obj_dest)
    WHERE is_deleted = false;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.sys_relation_map;");
        }
    }
}
