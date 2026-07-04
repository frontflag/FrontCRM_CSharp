using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>收款、应收、核销及客户预收相关表与字段注释。</summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260704130000_FinanceReceiptReceivableWriteOffComments")]
    public partial class FinanceReceiptReceivableWriteOffComments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
COMMENT ON TABLE public.financereceipt IS '收款单主表：客户到账记录，含审核与确认收款状态';
COMMENT ON TABLE public.financereceiptitem IS '收款明细表：收款单资金拆分行，核销与预收池以明细为粒度';
COMMENT ON TABLE public.finance_receivable IS '应收款表：销售出库完成后按出库单头生成，待收款核销';
COMMENT ON TABLE public.finance_receivable_write_off IS '应收核销流水：记录收款明细或预收池冲减应收的每笔金额';
COMMENT ON TABLE public.finance_customer_advance IS '客户预收余额表：按客户+币别汇总可复用预收资金';
COMMENT ON TABLE public.finance_customer_advance_ledger IS '客户预收流水表：预收入账、冲应收、超额转预收、退款等明细';

DO $$
DECLARE
    rec record;
BEGIN
    FOR rec IN
        SELECT *
        FROM (VALUES
            -- financereceipt
            ('financereceipt','FinanceReceiptId','收款单主键'),
            ('financereceipt','FinanceReceiptCode','收款单号（流水前缀 REC）'),
            ('financereceipt','CustomerId','客户ID'),
            ('financereceipt','CustomerName','客户名称（冗余）'),
            ('financereceipt','SalesUserId','业务员用户ID'),
            ('financereceipt','PurchaseGroupId','采购员组ID'),
            ('financereceipt','Status','收款状态：0草稿 1待审核 2已审核 3已收款 4已取消'),
            ('financereceipt','ReceiptAmount','收款总额'),
            ('financereceipt','ReceiptCurrency','收款币别：1人民币 2美元 3欧元'),
            ('financereceipt','ReceiptDate','收款日期'),
            ('financereceipt','ReceiptUserId','收款经办人用户ID'),
            ('financereceipt','ReceiptMode','收款方式：1银行转账 2现金 3支票 4承兑汇票'),
            ('financereceipt','ReceiptBankId','收款银行账户ID'),
            ('financereceipt','BankSlipNo','银行水单号码'),
            ('financereceipt','Remark','备注'),
            ('financereceipt','CreateTime','创建时间（UTC）'),
            ('financereceipt','CreateUserId','创建人用户ID（历史 bigint，逐步迁移至 create_by_user_id）'),
            ('financereceipt','ModifyTime','最后修改时间（UTC）'),
            ('financereceipt','ModifyUserId','最后修改人用户ID（历史 bigint）'),
            ('financereceipt','create_by_user_id','创建人用户ID（GUID）'),
            ('financereceipt','modify_by_user_id','最后修改人用户ID（GUID）'),
            ('financereceipt','is_deleted','软删除标记'),

            -- financereceiptitem
            ('financereceiptitem','FinanceReceiptItemId','收款明细主键'),
            ('financereceiptitem','FinanceReceiptId','收款单ID（外键）'),
            ('financereceiptitem','SellOrderId','关联销售订单ID（可选）'),
            ('financereceiptitem','SellOrderItemId','关联销售订单明细ID（可选）'),
            ('financereceiptitem','FinanceSellInvoiceId','关联销项发票ID（可选）'),
            ('financereceiptitem','FinanceSellInvoiceItemId','关联销项发票明细ID（可选）'),
            ('financereceiptitem','ReceiptAmount','收款金额'),
            ('financereceiptitem','ReceiptConvertAmount','收款折算金额（核销额度基准）'),
            ('financereceiptitem','VerifiedAmount','累计已核销金额（含冲减应收）'),
            ('financereceiptitem','StockOutItemId','关联出库明细ID（可选）'),
            ('financereceiptitem','ProductId','物料ID（可选）'),
            ('financereceiptitem','PN','物料型号（可选冗余）'),
            ('financereceiptitem','Brand','品牌（可选冗余）'),
            ('financereceiptitem','VerificationStatus','核销状态：0未核销 1部分核销 2核销完成'),
            ('financereceiptitem','ReceiptId','历史兼容收款单外键'),
            ('financereceiptitem','receipt_purpose','收款用途：10普通 20预收'),
            ('financereceiptitem','advance_sell_order_id','预收可选挂销售订单ID（软约束）'),
            ('financereceiptitem','advance_pool_amount','已转入客户预收池金额'),
            ('financereceiptitem','remark','备注'),
            ('financereceiptitem','is_deleted','软删除标记'),
            ('financereceiptitem','CreateTime','创建时间（UTC）'),
            ('financereceiptitem','CreateUserId','创建人用户ID'),
            ('financereceiptitem','ModifyTime','最后修改时间（UTC）'),
            ('financereceiptitem','ModifyUserId','最后修改人用户ID'),

            -- finance_receivable
            ('finance_receivable','FinanceReceivableId','应收款主键'),
            ('finance_receivable','ReceivableCode','应收单号（流水前缀 ARV）'),
            ('finance_receivable','stock_out_id','销售出库单ID（一出库一行，唯一）'),
            ('finance_receivable','StockOutCode','出库单号（冗余）'),
            ('finance_receivable','sell_order_id','销售订单ID'),
            ('finance_receivable','sell_order_code','销售订单号（冗余）'),
            ('finance_receivable','sell_order_item_id','销售订单明细ID'),
            ('finance_receivable','customer_id','客户ID'),
            ('finance_receivable','customer_name','客户名称（冗余）'),
            ('finance_receivable','sales_user_id','业务员用户ID'),
            ('finance_receivable','PN','物料型号（来自销售订单行）'),
            ('finance_receivable','Brand','品牌（来自销售订单行）'),
            ('finance_receivable','outbound_qty','出库数量'),
            ('finance_receivable','unit_price','销售单价'),
            ('finance_receivable','Currency','币别：1人民币 2美元 3欧元'),
            ('finance_receivable','Amount','应收金额（价税合计口径，与出库金额一致）'),
            ('finance_receivable','verified_done','累计已核销金额'),
            ('finance_receivable','verified_to_be','待核销金额'),
            ('finance_receivable','verification_status','核销状态：0未核销 1部分核销 2核销完成'),
            ('finance_receivable','stock_out_date','出库日期'),
            ('finance_receivable','is_deleted','软删除标记'),
            ('finance_receivable','CreateTime','创建时间（UTC）'),
            ('finance_receivable','ModifyTime','最后修改时间（UTC）'),
            ('finance_receivable','CreateByUserId','创建人用户ID'),
            ('finance_receivable','ModifyByUserId','最后修改人用户ID'),

            -- finance_receivable_write_off
            ('finance_receivable_write_off','FinanceReceivableWriteOffId','核销流水主键'),
            ('finance_receivable_write_off','finance_receivable_id','被冲减的应收款ID'),
            ('finance_receivable_write_off','finance_receipt_id','来源收款单ID（收款明细核销时）'),
            ('finance_receivable_write_off','finance_receipt_item_id','来源收款明细ID（收款明细核销时）'),
            ('finance_receivable_write_off','write_off_source','核销来源：10收款明细 20预收池'),
            ('finance_receivable_write_off','finance_customer_advance_ledger_id','关联预收流水ID（预收池核销时）'),
            ('finance_receivable_write_off','Amount','本次核销金额'),
            ('finance_receivable_write_off','operator_user_id','操作人用户ID'),
            ('finance_receivable_write_off','CreateTime','核销时间（UTC）'),
            ('finance_receivable_write_off','ModifyTime','最后修改时间（UTC）'),
            ('finance_receivable_write_off','CreateUserId','创建人用户ID'),
            ('finance_receivable_write_off','ModifyUserId','最后修改人用户ID'),

            -- finance_customer_advance
            ('finance_customer_advance','FinanceCustomerAdvanceId','客户预收账户主键'),
            ('finance_customer_advance','customer_id','客户ID'),
            ('finance_customer_advance','customer_name','客户名称（冗余）'),
            ('finance_customer_advance','Currency','币别：1人民币 2美元 3欧元'),
            ('finance_customer_advance','balance','当前可用预收余额'),
            ('finance_customer_advance','total_in','累计入账金额'),
            ('finance_customer_advance','total_applied','累计冲减应收金额'),
            ('finance_customer_advance','total_refund','累计退款金额'),
            ('finance_customer_advance','sales_user_id','业务员用户ID'),
            ('finance_customer_advance','is_deleted','软删除标记'),
            ('finance_customer_advance','CreateTime','创建时间（UTC）'),
            ('finance_customer_advance','ModifyTime','最后修改时间（UTC）'),

            -- finance_customer_advance_ledger
            ('finance_customer_advance_ledger','FinanceCustomerAdvanceLedgerId','预收流水主键'),
            ('finance_customer_advance_ledger','finance_customer_advance_id','客户预收账户ID'),
            ('finance_customer_advance_ledger','customer_id','客户ID'),
            ('finance_customer_advance_ledger','Currency','币别：1人民币 2美元 3欧元'),
            ('finance_customer_advance_ledger','ledger_type','流水类型：10入账 20冲应收 30超额转预收 40退款'),
            ('finance_customer_advance_ledger','Amount','流水金额（入账为正，冲减/退款为负或按业务约定）'),
            ('finance_customer_advance_ledger','finance_receipt_id','关联收款单ID'),
            ('finance_customer_advance_ledger','finance_receipt_item_id','关联收款明细ID'),
            ('finance_customer_advance_ledger','finance_receivable_id','关联应收款ID'),
            ('finance_customer_advance_ledger','finance_receivable_write_off_id','关联核销流水ID'),
            ('finance_customer_advance_ledger','sell_order_id','关联销售订单ID（可选）'),
            ('finance_customer_advance_ledger','Remark','备注'),
            ('finance_customer_advance_ledger','operator_user_id','操作人用户ID'),
            ('finance_customer_advance_ledger','CreateTime','流水时间（UTC）'),
            ('finance_customer_advance_ledger','ModifyTime','最后修改时间（UTC）'),
            ('finance_customer_advance_ledger','CreateUserId','创建人用户ID'),
            ('finance_customer_advance_ledger','ModifyUserId','最后修改人用户ID')
        ) AS t(table_name, column_name, comment_text)
    LOOP
        IF EXISTS (
            SELECT 1
            FROM information_schema.columns c
            WHERE c.table_schema = 'public'
              AND c.table_name = rec.table_name
              AND c.column_name = rec.column_name
        ) THEN
            EXECUTE format(
                'COMMENT ON COLUMN public.%I.%I IS %L',
                rec.table_name,
                rec.column_name,
                rec.comment_text
            );
        END IF;
    END LOOP;
END $$;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 注释为文档元数据，不做自动回滚
        }
    }
}
