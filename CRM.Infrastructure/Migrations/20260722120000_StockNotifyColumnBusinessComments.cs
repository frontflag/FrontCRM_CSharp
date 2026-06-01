using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations;

/// <summary>
/// 为 public.stockout_notify、public.stockin_notify 全部列补充 PostgreSQL 业务列注释。
/// 可重复执行脚本见 scripts/stockout_notify_stockin_notify_column_business_comments_postgresql.sql。
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260722120000_StockNotifyColumnBusinessComments")]
public partial class StockNotifyColumnBusinessComments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            COMMENT ON TABLE public.stockout_notify IS '出库通知（单表：一条通知对应一条销售订单明细的一次申请出库）';
            COMMENT ON COLUMN public.stockout_notify."ID" IS '出库通知主键（GUID）；packing_item.stockout_notify_id、customs_declaration.StockOutRequestId 等外键指向此列';
            COMMENT ON COLUMN public.stockout_notify."Code" IS '出库通知业务单号（模块 StockOutRequest/STOR 流水，如 STOR00001）；列表、打印与下游装箱关联';
            COMMENT ON COLUMN public.stockout_notify."SalesOrderId" IS '所属销售订单主键（GUID），关联 sellorder.SellOrderId';
            COMMENT ON COLUMN public.stockout_notify."SalesOrderItemId" IS '所属销售订单明细主键（GUID），关联 sellorderitem.SellOrderItemId；本表一条记录仅绑定一行销售明细';
            COMMENT ON COLUMN public.stockout_notify."MaterialCode" IS '申请出库物料型号（PN）快照；创建时默认取自销售明细 pn，后续不随销售行变更自动刷新';
            COMMENT ON COLUMN public.stockout_notify."MaterialName" IS '申请出库品牌快照；创建时默认取自销售明细 brand';
            COMMENT ON COLUMN public.stockout_notify."Quantity" IS '本通知申请出库数量（整数）；与同销售明细其它未取消通知数量合计不得超过该行可出库余量';
            COMMENT ON COLUMN public.stockout_notify."CustomerId" IS '发货客户主键（GUID），关联 customerinfo；创建时由请求体或销售订单客户解析写入';
            COMMENT ON COLUMN public.stockout_notify."RequestUserId" IS '申请人用户主键（GUID，与 JWT 登录用户一致）';
            COMMENT ON COLUMN public.stockout_notify."RequestDate" IS '申请/计划出库日期（timestamptz，存 UTC）';
            COMMENT ON COLUMN public.stockout_notify."Status" IS '出库通知状态（StockOutRequestStatusCode）：10=待装箱 20=已装箱 100=已出库 -1=已取消；取消后不可再加入装箱篮子';
            COMMENT ON COLUMN public.stockout_notify."Remark" IS '业务备注（自由文本）';
            COMMENT ON COLUMN public.stockout_notify."ShipmentMethod" IS '出货方式：数据字典 LogisticsArrivalMethod 的 ItemCode（与物流「来货方式」同源，存编码非展示名）';
            COMMENT ON COLUMN public.stockout_notify."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；与仓库、入库单、出库单共用枚举，影响物流/报关场景判断';
            COMMENT ON COLUMN public.stockout_notify."StockOutType" IS '出库业务类型（StockOutTypeCode）：10=销售出库 20=报关出库 30=退货出库 40=报废出库；非法值归一为 10；装箱单要求同批通知类型一致';
            COMMENT ON COLUMN public.stockout_notify."CreateTime" IS '记录创建时间（UTC）';
            COMMENT ON COLUMN public.stockout_notify."CreateUserId" IS '历史创建人 bigint（旧体系审计字段；业务追溯请优先 create_by_user_id）';
            COMMENT ON COLUMN public.stockout_notify."ModifyTime" IS '记录最后修改时间（UTC）';
            COMMENT ON COLUMN public.stockout_notify."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';
            COMMENT ON COLUMN public.stockout_notify.create_by_user_id IS '创建本通知时的登录用户 GUID（JWT 用户主键，审计与权限追溯）';
            COMMENT ON COLUMN public.stockout_notify.modify_by_user_id IS '最后修改本通知时的登录用户 GUID';

            COMMENT ON TABLE public.stockin_notify IS '到货通知（单表：一条记录 = 采购订单明细上的一次到货批次）';
            COMMENT ON COLUMN public.stockin_notify."UserId" IS '到货通知主键（GUID）；qcinfo.StockInNotifyId、入库单 SourceId 等外键指向此列（列名 UserId 为历史命名）';
            COMMENT ON COLUMN public.stockin_notify."NoticeCode" IS '到货通知业务单号（模块 ArrivalNotice 流水）；列表展示与质检单 StockInNotifyCode 冗余关联';
            COMMENT ON COLUMN public.stockin_notify."PurchaseOrderId" IS '所属采购订单主键（GUID），关联 purchaseorder';
            COMMENT ON COLUMN public.stockin_notify."PurchaseOrderCode" IS '采购订单号冗余（创建时从采购主单复制，减少列表联表）';
            COMMENT ON COLUMN public.stockin_notify."PurchaseOrderItemId" IS '所属采购订单明细主键（GUID）；同一采购行可有多条到货通知（分批到货）';
            COMMENT ON COLUMN public.stockin_notify."SellOrderItemId" IS '关联销售订单明细主键（GUID，冗余自采购明细）；用于追溯销售需求与扩展表回算';
            COMMENT ON COLUMN public.stockin_notify."VendorId" IS '供应商主键（GUID，创建时从采购订单复制）';
            COMMENT ON COLUMN public.stockin_notify."VendorName" IS '供应商名称冗余（展示用）';
            COMMENT ON COLUMN public.stockin_notify."PurchaseUserName" IS '采购业务员名称冗余（来自采购订单，展示用）';
            COMMENT ON COLUMN public.stockin_notify."Status" IS '到货通知状态：1=新建（遗留）10=未到货 20=到货待检 30=已质检 100=已入库；由质检/入库及 purchaseorderitemextend 同步回写';
            COMMENT ON COLUMN public.stockin_notify."ExpectedArrivalDate" IS '预计到货日期（timestamptz）；创建时默认取采购明细或采购主单交货日';
            COMMENT ON COLUMN public.stockin_notify."RegionType" IS '地域类型（RegionTypeCode）：10=境内 20=境外；与仓库档案、入库单 RegionType 语义一致';
            COMMENT ON COLUMN public.stockin_notify."StockInType" IS '入库业务类型（StockInTypeCode）：10=采购入库 20=报关入库 30=退货入库 40=报废入库；下游 stock_in.StockInType 可取自此字段';
            COMMENT ON COLUMN public.stockin_notify."Pn" IS '物料型号（PN）快照，创建时取自采购明细';
            COMMENT ON COLUMN public.stockin_notify."Brand" IS '品牌快照，创建时取自采购明细';
            COMMENT ON COLUMN public.stockin_notify."ExpectQty" IS '本批次预期到货数量（整数）；创建时不得超过采购行扩展 QtyStockInNotifyNot';
            COMMENT ON COLUMN public.stockin_notify."ReceiveQty" IS '本批次实收到货数量；质检/收货流程回写，参与采购行「剩余可通知」余量计算';
            COMMENT ON COLUMN public.stockin_notify."PassedQty" IS '本批次质检通过数量汇总（各质检明细通过量合计回写）';
            COMMENT ON COLUMN public.stockin_notify."Cost" IS '采购单价快照（numeric(18,6)，创建时取自采购明细 cost）';
            COMMENT ON COLUMN public.stockin_notify."ExpectTotal" IS '预期到货金额（numeric(18,2)），创建时按 ExpectQty×Cost 四舍五入';
            COMMENT ON COLUMN public.stockin_notify."ReceiveTotal" IS '实收金额（numeric(18,2)），随收货/入库流程回写';
            COMMENT ON COLUMN public.stockin_notify."CreateTime" IS '记录创建时间（UTC）';
            COMMENT ON COLUMN public.stockin_notify."CreateUserId" IS '历史创建人 bigint（旧体系审计字段）';
            COMMENT ON COLUMN public.stockin_notify."ModifyTime" IS '记录最后修改时间（UTC）';
            COMMENT ON COLUMN public.stockin_notify."ModifyUserId" IS '历史最后修改人 bigint（旧体系审计字段）';
            """);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'stockout_notify' AND column_name = 'is_deleted'
              ) THEN
                EXECUTE $c$COMMENT ON COLUMN public.stockout_notify.is_deleted IS '软删除标记：true 表示逻辑删除，EF HasQueryFilter 常规查询应排除';$c$;
              END IF;

              IF EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'stockin_notify' AND column_name = 'is_deleted'
              ) THEN
                EXECUTE $c$COMMENT ON COLUMN public.stockin_notify.is_deleted IS '软删除标记：true 表示逻辑删除，EF HasQueryFilter 常规查询应排除';$c$;
              END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 列注释为文档元数据，不做自动回滚
    }
}
