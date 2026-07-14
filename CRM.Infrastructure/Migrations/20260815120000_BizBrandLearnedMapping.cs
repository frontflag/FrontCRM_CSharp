using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>品牌导入学习映射表 biz_brand_learned_mapping。</summary>
[Migration("20260815120000_BizBrandLearnedMapping")]
public partial class BizBrandLearnedMapping : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
CREATE TABLE IF NOT EXISTS public.biz_brand_learned_mapping (
    id BIGSERIAL PRIMARY KEY,
    source_text VARCHAR(500) NOT NULL,
    source_key VARCHAR(500) NOT NULL,
    brand_id BIGINT NOT NULL,
    hit_count INTEGER NOT NULL DEFAULT 1,
    last_used_by_user_id VARCHAR(36) NULL,
    create_by_user_id VARCHAR(36) NULL,
    create_time TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    update_time TIMESTAMPTZ NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_biz_brand_learned_mapping_source_key
    ON public.biz_brand_learned_mapping (source_key);

CREATE INDEX IF NOT EXISTS ix_biz_brand_learned_mapping_brand_id
    ON public.biz_brand_learned_mapping (brand_id);
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS public.biz_brand_learned_mapping;");
    }
}
