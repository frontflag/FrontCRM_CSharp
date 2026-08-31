using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>报关公司：装箱单收货人联系人 / 电话 / 邮箱 / 地址。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260901080000_CustomsBrokerPrintConsignee")]
    public partial class CustomsBrokerPrintConsignee : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_broker
                    ADD COLUMN IF NOT EXISTS contact_name character varying(100) NULL,
                    ADD COLUMN IF NOT EXISTS tel character varying(64) NULL,
                    ADD COLUMN IF NOT EXISTS email character varying(200) NULL,
                    ADD COLUMN IF NOT EXISTS address character varying(500) NULL;

                COMMENT ON COLUMN public.customs_broker.contact_name IS '装箱单收货人联系人';
                COMMENT ON COLUMN public.customs_broker.tel IS '装箱单收货人电话';
                COMMENT ON COLUMN public.customs_broker.email IS '装箱单收货人邮箱';
                COMMENT ON COLUMN public.customs_broker.address IS '装箱单收货人地址（按需印出的原文）';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.customs_broker
                    DROP COLUMN IF EXISTS contact_name,
                    DROP COLUMN IF EXISTS tel,
                    DROP COLUMN IF EXISTS email,
                    DROP COLUMN IF EXISTS address;
                """);
        }
    }
}
