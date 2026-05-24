using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>装箱单收发货地址扩展表 packing_extend_ship。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260518140000_AddPackingExtendShip")]
    public partial class AddPackingExtendShip : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS public.packing_extend_ship (
    ""Id"" character varying(36) NOT NULL,
    ""PackingId"" character varying(36) NOT NULL,
    ship_company character varying(200) NULL,
    ship_address character varying(256) NULL,
    ship_attn character varying(100) NULL,
    ship_tel character varying(64) NULL,
    bill_company character varying(200) NULL,
    bill_address character varying(256) NULL,
    bill_attn character varying(100) NULL,
    bill_tel character varying(64) NULL,
    delivery_req character varying(256) NULL,
    delivery_method smallint NULL,
    CONSTRAINT ""PK_packing_extend_ship"" PRIMARY KEY (""Id"")
);

CREATE UNIQUE INDEX IF NOT EXISTS ""IX_packing_extend_ship_PackingId"" ON public.packing_extend_ship (""PackingId"");

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_packing_extend_ship_packing_PackingId') THEN
        ALTER TABLE public.packing_extend_ship
            ADD CONSTRAINT ""FK_packing_extend_ship_packing_PackingId""
            FOREIGN KEY (""PackingId"") REFERENCES public.packing (""Id"") ON DELETE CASCADE;
    END IF;
END $$;

COMMENT ON TABLE public.packing_extend_ship IS '装箱单收发货地址扩展';
COMMENT ON COLUMN public.packing_extend_ship.ship_company IS '送货地址公司名称';
COMMENT ON COLUMN public.packing_extend_ship.ship_address IS '送货地址';
COMMENT ON COLUMN public.packing_extend_ship.ship_attn IS '送货联系人';
COMMENT ON COLUMN public.packing_extend_ship.ship_tel IS '送货联系人电话';
COMMENT ON COLUMN public.packing_extend_ship.bill_company IS '账单地址公司名称';
COMMENT ON COLUMN public.packing_extend_ship.bill_address IS '账单地址';
COMMENT ON COLUMN public.packing_extend_ship.bill_attn IS '账单联系人';
COMMENT ON COLUMN public.packing_extend_ship.bill_tel IS '账单联系人电话';
COMMENT ON COLUMN public.packing_extend_ship.delivery_req IS '送货要求';
COMMENT ON COLUMN public.packing_extend_ship.delivery_method IS '送货方式：10送货 20自提';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.packing_extend_ship;");
        }
    }
}
