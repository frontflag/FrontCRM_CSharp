using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 为 public.sellorder 全部列补充 PostgreSQL 列注释（业务含义 + 与代码一致的枚举说明）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260710120000_SellOrderColumnBusinessComments")]
    public partial class SellOrderColumnBusinessComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                COMMENT ON COLUMN public.sellorder."SellOrderId" IS '销售订单主键（GUID），全库唯一标识本单';
                COMMENT ON COLUMN public.sellorder.sell_order_code IS '销售单号（业务编号），用于展示、检索与打印，全局唯一';
                COMMENT ON COLUMN public.sellorder.customer_id IS '客户主键（GUID），关联客户主数据';
                COMMENT ON COLUMN public.sellorder.customer_name IS '客户名称快照（下单时冗余，便于列表与历史单据展示）';
                COMMENT ON COLUMN public.sellorder.sales_user_id IS '业务员用户 GUID（与登录用户/用户主表一致）';
                COMMENT ON COLUMN public.sellorder.sales_user_name IS '业务员姓名快照（冗余展示）';
                COMMENT ON COLUMN public.sellorder.purchase_group_id IS '采购组/采购组别标识（GUID），用于分配采购职责';
                COMMENT ON COLUMN public.sellorder.status IS '订单主状态（与 SellOrderMainStatus 一致）：1新建 2待审核 10审核通过 20进行中 100完成 -1审核失败 -2取消';
                COMMENT ON COLUMN public.sellorder.err_status IS '异常/差错状态码（0 表示无异常；非 0 为业务异常标记，具体含义由业务逻辑定义）';
                COMMENT ON COLUMN public.sellorder.type IS '订单类型：1客单采购 2备货采购 3样品采购';
                COMMENT ON COLUMN public.sellorder.currency IS '订单币别（与 CurrencyCode 一致）：1=RMB 2=USD 3=EUR 4=HKD 5=JPY 6=GBP';
                COMMENT ON COLUMN public.sellorder.total IS '订单金额合计（原币，与明细汇总口径一致的业务总额）';
                COMMENT ON COLUMN public.sellorder.convert_total IS '折合本位币（如人民币）的订单总额，用于汇总与报表';
                COMMENT ON COLUMN public.sellorder.item_rows IS '订单明细行数（有效行计数，具体是否含取消行以业务刷新逻辑为准）';
                COMMENT ON COLUMN public.sellorder.purchase_order_status IS '关联采购执行汇总：0未采购 1部分采购 2全部采购';
                COMMENT ON COLUMN public.sellorder.stock_out_status IS '销售出库汇总：0未出库 1部分出库 2全部出库';
                COMMENT ON COLUMN public.sellorder.stock_in_status IS '销售相关入库汇总（含退货等）：0未入库 1部分入库 2全部入库';
                COMMENT ON COLUMN public.sellorder.finance_receipt_status IS '收款执行汇总：0未收款 1部分收款 2全部收款';
                COMMENT ON COLUMN public.sellorder.finance_payment_status IS '付款执行汇总：0未付款 1部分付款 2全部付款';
                COMMENT ON COLUMN public.sellorder.invoice_status IS '开票执行汇总：0未开票 1部分开票 2全部开票';
                COMMENT ON COLUMN public.sellorder.delivery_address IS '送货/收货地址（客户交付信息）';
                COMMENT ON COLUMN public.sellorder.delivery_date IS '约定或计划交货日期（时区为 timestamptz）';
                COMMENT ON COLUMN public.sellorder.comment IS '订单备注（业务说明、客户要求等自由文本）';
                COMMENT ON COLUMN public.sellorder.audit_remark IS '审核说明：审核不通过原因或审批人补充说明（审核失败时常由审批人填写）';
                COMMENT ON COLUMN public.sellorder."CreateTime" IS '记录创建时间（UTC）';
                COMMENT ON COLUMN public.sellorder."CreateUserId" IS '历史创建人 bigint（旧体系字段，与当前 JWT 用户 GUID 不一致，勿用于业务关联；请优先使用 create_by_user_id）';
                COMMENT ON COLUMN public.sellorder."ModifyTime" IS '记录最后修改时间（UTC）';
                COMMENT ON COLUMN public.sellorder."ModifyUserId" IS '历史最后修改人 bigint（旧体系字段；请优先使用 modify_by_user_id）';
                COMMENT ON COLUMN public.sellorder.create_by_user_id IS '创建本单时的登录用户 GUID（与 user 表主键及 JWT 一致，用于审计与权限追溯）';
                COMMENT ON COLUMN public.sellorder.modify_by_user_id IS '最后修改本单时的登录用户 GUID';
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                  IF EXISTS (
                    SELECT 1
                    FROM information_schema.columns c
                    WHERE c.table_schema = 'public'
                      AND c.table_name = 'sellorder'
                      AND c.column_name = 'is_deleted'
                  ) THEN
                    EXECUTE $c$COMMENT ON COLUMN public.sellorder.is_deleted IS '软删除标记：true 表示逻辑删除，常规列表与业务查询应过滤';$c$;
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
