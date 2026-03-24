using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 为本地 PostgreSQL 业务表和字段补充注释（含关键枚举字段定义）。
    /// 说明：
    /// 1) 先补充关键业务表的精确注释与状态枚举说明；
    /// 2) 再对 public schema 下无注释的表/字段按命名规则自动补齐通用注释。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324193000_AddDatabaseFieldComments")]
    public partial class AddDatabaseFieldComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- =============================
-- 1) 关键业务表：精确注释
-- =============================
COMMENT ON TABLE public.purchaseorder IS '采购订单主表';
COMMENT ON COLUMN public.purchaseorder.""status"" IS '订单状态：1新建 2待审核 10审核通过 20待确认 30已确认 50进行中 100采购完成 -1审核失败 -2取消';
COMMENT ON COLUMN public.purchaseorder.""type"" IS '订单类型：1普通 2紧急 3样品';
COMMENT ON COLUMN public.purchaseorder.""currency"" IS '币别：1CNY 2USD 3EUR';
COMMENT ON COLUMN public.purchaseorder.""stock_status"" IS '入库状态：0未入库 1部分入库 2全部入库';
COMMENT ON COLUMN public.purchaseorder.""finance_status"" IS '付款状态：0未付款 1部分付款 2全部付款';
COMMENT ON COLUMN public.purchaseorder.""stock_out_status"" IS '出库状态：0未出库 1部分出库 2全部出库';
COMMENT ON COLUMN public.purchaseorder.""invoice_status"" IS '开票状态：0未开票 1部分开票 2全部开票';

COMMENT ON TABLE public.purchaseorderitem IS '采购订单明细表';
COMMENT ON COLUMN public.purchaseorderitem.""status"" IS '明细状态：1新建 2待审核 10审核通过 20待确认 30已确认 40已付款 50已发货 60已入库 100采购完成 -1审核失败 -2取消';
COMMENT ON COLUMN public.purchaseorderitem.""currency"" IS '币别：1CNY 2USD 3EUR';
COMMENT ON COLUMN public.purchaseorderitem.""stock_in_status"" IS '入库状态：0未入库 1部分入库 2全部入库';
COMMENT ON COLUMN public.purchaseorderitem.""finance_payment_status"" IS '付款状态：0未付款 1部分付款 2全部付款';
COMMENT ON COLUMN public.purchaseorderitem.""stock_out_status"" IS '出库状态：0未出库 1部分出库 2全部出库';

COMMENT ON TABLE public.stockinnotify IS '到货通知主表';
COMMENT ON COLUMN public.stockinnotify.""Status"" IS '到货通知状态：1新建 10未到货 20到货待检 30已质检 100已入库';

COMMENT ON TABLE public.stockinnotifyitem IS '到货通知明细表';
COMMENT ON COLUMN public.stockinnotifyitem.""Qty"" IS '采购数量';
COMMENT ON COLUMN public.stockinnotifyitem.""ArrivedQty"" IS '到货数量';
COMMENT ON COLUMN public.stockinnotifyitem.""PassedQty"" IS '质检通过数量';

COMMENT ON TABLE public.qcinfo IS '质检主表';
COMMENT ON COLUMN public.qcinfo.""Status"" IS '质检结果：-1未通过 10部分通过 100已通过';
COMMENT ON COLUMN public.qcinfo.""StockInStatus"" IS '入库状态：-1拒收 1未入库 10部分入库 100全部入库';
COMMENT ON COLUMN public.qcinfo.""PassQty"" IS '通过数量';
COMMENT ON COLUMN public.qcinfo.""RejectQty"" IS '拒收数量';

COMMENT ON TABLE public.qcitem IS '质检明细表';
COMMENT ON COLUMN public.qcitem.""ArrivedQty"" IS '到货数量';
COMMENT ON COLUMN public.qcitem.""PassedQty"" IS '通过数量';
COMMENT ON COLUMN public.qcitem.""RejectQty"" IS '拒收数量';

COMMENT ON TABLE public.stockin IS '入库单主表';
COMMENT ON COLUMN public.stockin.""Status"" IS '入库单状态：0草稿 1待入库 2已入库 3已取消';
COMMENT ON COLUMN public.stockin.""InspectStatus"" IS '质检状态：0未质检 1合格 2不合格';

COMMENT ON TABLE public.stockout IS '出库单主表';
COMMENT ON COLUMN public.stockout.""Status"" IS '出库单状态：0草稿 1待出库 2已出库 3已取消';

COMMENT ON TABLE public.purchaserequisition IS '采购申请单主表';
COMMENT ON COLUMN public.purchaserequisition.""status"" IS '采购申请状态：0草稿 1待审核 10审核通过 20待下单 30部分下单 100已完成 -1审核失败 -2取消';

-- =============================
-- 2) 通用补齐：对无注释表/字段按命名规则自动添加
-- =============================
DO $$
DECLARE
    r record;
    v_comment text;
BEGIN
    -- 表注释
    FOR r IN
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
    LOOP
        IF obj_description(format('%I.%I', r.table_schema, r.table_name)::regclass, 'pg_class') IS NULL THEN
            EXECUTE format('COMMENT ON TABLE %I.%I IS %L',
                r.table_schema,
                r.table_name,
                '业务表：' || r.table_name);
        END IF;
    END LOOP;

    -- 字段注释
    FOR r IN
        SELECT c.table_schema, c.table_name, c.column_name
        FROM information_schema.columns c
        WHERE c.table_schema = 'public'
        ORDER BY c.table_name, c.ordinal_position
    LOOP
        IF col_description(format('%I.%I', r.table_schema, r.table_name)::regclass,
                           (SELECT ordinal_position::int
                            FROM information_schema.columns
                            WHERE table_schema = r.table_schema
                              AND table_name = r.table_name
                              AND column_name = r.column_name)) IS NULL THEN
            v_comment :=
                CASE lower(r.column_name)
                    WHEN 'id' THEN '主键ID'
                    WHEN 'status' THEN '状态字段（具体枚举见业务定义）'
                    WHEN 'type' THEN '类型字段（具体枚举见业务定义）'
                    WHEN 'currency' THEN '币别字段（具体枚举见业务定义）'
                    WHEN 'createtime' THEN '创建时间'
                    WHEN 'create_time' THEN '创建时间'
                    WHEN 'modifytime' THEN '修改时间'
                    WHEN 'modify_time' THEN '修改时间'
                    WHEN 'createuserid' THEN '创建人ID'
                    WHEN 'create_user_id' THEN '创建人ID'
                    WHEN 'modifyuserid' THEN '修改人ID'
                    WHEN 'modify_user_id' THEN '修改人ID'
                    WHEN 'remark' THEN '备注'
                    WHEN 'comment' THEN '备注'
                    WHEN 'inner_comment' THEN '内部备注'
                    WHEN 'code' THEN '业务编码'
                    WHEN 'name' THEN '名称'
                    WHEN 'qty' THEN '数量'
                    WHEN 'quantity' THEN '数量'
                    WHEN 'price' THEN '单价'
                    WHEN 'amount' THEN '金额'
                    WHEN 'total' THEN '总计金额'
                    WHEN 'totalamount' THEN '总金额'
                    WHEN 'totalquantity' THEN '总数量'
                    ELSE
                        CASE
                            WHEN lower(r.column_name) LIKE '%_id' OR lower(r.column_name) LIKE '%id'
                                THEN '关联ID：' || r.column_name
                            WHEN lower(r.column_name) LIKE '%_code' OR lower(r.column_name) LIKE '%code'
                                THEN '业务编码：' || r.column_name
                            WHEN lower(r.column_name) LIKE '%_name' OR lower(r.column_name) LIKE '%name'
                                THEN '名称：' || r.column_name
                            WHEN lower(r.column_name) LIKE '%date%' OR lower(r.column_name) LIKE '%time%'
                                THEN '时间字段：' || r.column_name
                            WHEN lower(r.column_name) LIKE '%status%'
                                THEN '状态字段：' || r.column_name || '（具体枚举见业务定义）'
                            ELSE '业务字段：' || r.column_name
                        END
                END;

            EXECUTE format('COMMENT ON COLUMN %I.%I.%I IS %L',
                r.table_schema, r.table_name, r.column_name, v_comment);
        END IF;
    END LOOP;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 注释为文档性元数据，回滚时不自动清理，避免影响人工维护内容
        }
    }
}

