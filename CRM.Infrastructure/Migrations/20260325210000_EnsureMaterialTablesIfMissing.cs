using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 物料分类 + 物料主表（库存中心按 MaterialId 关联展示型号/名称）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260325210000_EnsureMaterialTablesIfMissing")]
    public partial class EnsureMaterialTablesIfMissing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.materialcategory (
    ""CategoryId"" character varying(36) NOT NULL,
    ""CategoryCode"" character varying(50) NOT NULL,
    ""CategoryName"" character varying(100) NOT NULL,
    ""ParentId"" character varying(36) NULL,
    ""Level"" smallint NOT NULL DEFAULT 1,
    ""SortOrder"" integer NOT NULL DEFAULT 0,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""Remark"" character varying(500) NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_materialcategory"" PRIMARY KEY (""CategoryId"")
);

CREATE TABLE IF NOT EXISTS public.material (
    ""MaterialId"" character varying(36) NOT NULL,
    ""MaterialCode"" character varying(50) NOT NULL,
    ""MaterialName"" character varying(200) NOT NULL,
    ""MaterialModel"" character varying(100) NULL,
    ""BrandId"" character varying(36) NULL,
    ""CategoryId"" character varying(36) NULL,
    ""Unit"" character varying(20) NULL,
    ""Status"" smallint NOT NULL DEFAULT 1,
    ""Remark"" character varying(500) NULL,
    ""CreateTime"" timestamp with time zone NOT NULL,
    ""CreateUserId"" bigint NULL,
    ""ModifyTime"" timestamp with time zone NULL,
    ""ModifyUserId"" bigint NULL,
    CONSTRAINT ""PK_material"" PRIMARY KEY (""MaterialId"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_material_MaterialCode"" ON public.material (""MaterialCode"");
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
