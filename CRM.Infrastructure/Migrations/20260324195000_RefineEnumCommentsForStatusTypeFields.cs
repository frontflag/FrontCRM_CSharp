using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <summary>
    /// 精确补充状态/类型/币别等枚举字段注释（财务、销售等核心模块）。
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260324195000_RefineEnumCommentsForStatusTypeFields")]
    public partial class RefineEnumCommentsForStatusTypeFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE
    rec record;
BEGIN
    FOR rec IN
        SELECT *
        FROM (VALUES
            -- financepayment
            ('financepayment','Status','付款状态：1新建 2待审核 10审核通过 100付款完成 -1审核失败 -2取消'),
            ('financepayment','PaymentCurrency','付款币别：1人民币 2美元 3欧元'),
            ('financepayment','PaymentMode','付款方式：1银行转账 2现金 3支票 4承兑汇票'),

            -- financepaymentitem
            ('financepaymentitem','VerificationStatus','核销状态：0未核销 1部分核销 2核销完成'),

            -- financereceipt
            ('financereceipt','Status','收款状态：0草稿 1待审核 2已审核 3已收款 4已取消'),
            ('financereceipt','ReceiptCurrency','收款币别：1人民币 2美元 3欧元'),
            ('financereceipt','ReceiptMode','收款方式：1银行转账 2现金 3支票 4承兑汇票'),

            -- financereceiptitem
            ('financereceiptitem','VerificationStatus','核销状态：0未核销 1部分核销 2核销完成'),

            -- financepurchaseinvoice
            ('financepurchaseinvoice','ConfirmStatus','认证状态：0未认证 1已认证'),
            ('financepurchaseinvoice','RedInvoiceStatus','冲红状态：0正常 1已冲红'),

            -- financesellinvoice
            ('financesellinvoice','ReceiveStatus','收款状态：0未收款 1部分收款 2收款完成'),
            ('financesellinvoice','Currency','币别：1人民币 2美元 3欧元'),
            ('financesellinvoice','Type','发票类型：10蓝字发票 20红字发票'),
            ('financesellinvoice','InvoiceStatus','发票状态：1未申请 2申请中 100已开票 101开票失败 -1已作废'),
            ('financesellinvoice','SellInvoiceType','销项发票类别：100增值税专用发票 200增值税普通发票'),

            -- sellinvoiceitem
            ('sellinvoiceitem','ReceiveStatus','收款状态：0未收款 1部分收款 2收款完成'),
            ('sellinvoiceitem','Currency','币别：1人民币 2美元 3欧元'),

            -- sellorder
            ('sellorder','status','订单状态：1新建 2待审核 10审核通过 20进行中 100完成 -1审核失败 -2取消'),
            ('sellorder','type','订单类型：1普通 2紧急 3样品'),
            ('sellorder','currency','币别：1RMB 2USD 3EUR'),
            ('sellorder','purchase_order_status','采购状态：0未采购 1部分采购 2全部采购'),
            ('sellorder','stock_out_status','出库状态：0未出库 1部分出库 2全部出库'),
            ('sellorder','stock_in_status','入库状态：0未入库 1部分入库 2全部入库'),
            ('sellorder','finance_receipt_status','收款状态：0未收款 1部分收款 2全部收款'),
            ('sellorder','finance_payment_status','付款状态：0未付款 1部分付款 2全部付款'),
            ('sellorder','invoice_status','开票状态：0未开票 1部分开票 2全部开票'),

            -- sellorderitem
            ('sellorderitem','status','明细状态：0正常 1已取消'),
            ('sellorderitem','currency','币别：1RMB 2USD 3EUR'),

            -- payment（旧付款模型）
            ('payment','Status','状态：0草稿 1待审核 2已审核 3已付款 4已取消'),
            ('payment','PaymentType','付款类型：1采购付款 2费用付款 3预付款 4其他'),
            ('payment','PaymentMethod','付款方式：1银行转账 2现金 3支票 4承兑汇票 5信用证'),
            ('payment','Currency','币别：1人民币 2美元 3欧元'),

            -- receipt（旧收款模型）
            ('receipt','Status','状态：0草稿 1待审核 2已审核 3已收款 4已取消'),
            ('receipt','ReceiptType','收款类型：1销售收款 2预收款 3退款 4其他'),
            ('receipt','ReceiptMethod','收款方式：1银行转账 2现金 3支票 4承兑汇票 5信用证 6支付宝 7微信'),
            ('receipt','Currency','币别：1人民币 2美元 3欧元')
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

