using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 为 public.sellorderitem 全部列补充 PostgreSQL 列注释（业务含义 + 与代码一致的枚举说明）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260715120000_SellOrderItemColumnBusinessComments")]
    public partial class SellOrderItemColumnBusinessComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                COMMENT ON COLUMN public.sellorderitem."SellOrderItemId" IS '销售订单明细主键（GUID），与扩展表 sellorderitemextend 主键一致，全库唯一标识本行';
                COMMENT ON COLUMN public.sellorderitem.sell_order_id IS '所属销售订单主键（GUID），外键关联 sellorder.SellOrderId，级联归属';
                COMMENT ON COLUMN public.sellorderitem.sell_order_item_code IS '销售明细业务编号（如 销售单号-行序），同一订单内唯一，用于展示、打印与下游单据关联';
                COMMENT ON COLUMN public.sellorderitem.quote_id IS '来源报价单主键（GUID）；由报价转单时写入，便于追溯报价行与成本快照';
                COMMENT ON COLUMN public.sellorderitem.product_id IS '物料/商品主数据主键（GUID）；与 PN+品牌 可并存，用于关联产品档案';
                COMMENT ON COLUMN public.sellorderitem.pn IS '物料型号（Part Number），销售与采购匹配的核心标识之一';
                COMMENT ON COLUMN public.sellorderitem.brand IS '品牌；与 PN 组合定位具体物料，用于采购、库存与报表';
                COMMENT ON COLUMN public.sellorderitem.customer_pn_no IS '客户侧料号/客户物料编号（冗余展示与对账）';
                COMMENT ON COLUMN public.sellorderitem.qty IS '本行销售数量（numeric(18,4)），为销售履约与金额计算基础';
                COMMENT ON COLUMN public.sellorderitem.purchased_qty IS '已下推/已关联采购的数量汇总（实时或定时刷新），用于判断采购进度';
                COMMENT ON COLUMN public.sellorderitem.price IS '销售单价（原币，numeric(18,6)）；行金额通常按 qty×price 汇总入订单总额';
                COMMENT ON COLUMN public.sellorderitem.convert_price IS '销售单价折合美元快照（按财务参数 USD 基准汇率计算），用于跨币别分析与报表';
                COMMENT ON COLUMN public.sellorderitem.currency IS '本行销售币别（与 CurrencyCode 一致）：1=RMB 2=USD 3=EUR 4=HKD 5=JPY 6=GBP';
                COMMENT ON COLUMN public.sellorderitem.date_code IS '生产日期/批次代码要求（客户或业务约定的 DC/Lot 等文本）';
                COMMENT ON COLUMN public.sellorderitem.delivery_date IS '本行约定或计划交货日期（timestamptz）';
                COMMENT ON COLUMN public.sellorderitem.status IS '明细业务状态：0=正常 1=已取消（取消行一般不再参与采购/出库有效量）';
                COMMENT ON COLUMN public.sellorderitem.comment IS '本行备注（自由文本；可含客户物料型号前缀等业务约定格式）';
                COMMENT ON COLUMN public.sellorderitem."CreateTime" IS '明细行创建时间（UTC）';
                COMMENT ON COLUMN public.sellorderitem."CreateUserId" IS '历史创建人 bigint（旧体系审计字段；与当前 JWT GUID 用户体系可能不一致）';
                COMMENT ON COLUMN public.sellorderitem."ModifyTime" IS '明细行最后修改时间（UTC）';
                COMMENT ON COLUMN public.sellorderitem."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns c
                    WHERE c.table_schema = 'public'
                      AND c.table_name = 'sellorderitem'
                      AND c.column_name = 'is_deleted'
                  ) THEN
                    EXECUTE $c$COMMENT ON COLUMN public.sellorderitem.is_deleted IS '软删除标记：true 表示逻辑删除，常规列表与 HasQueryFilter 应过滤';$c$;
                  END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 列注释为文档元数据，不做自动回滚
        }
    }
}
