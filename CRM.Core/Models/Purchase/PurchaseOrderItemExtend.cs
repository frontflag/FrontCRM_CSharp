using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Models.Purchase
{
    /// <summary>
    /// 采购明细扩展：与 <see cref="PurchaseOrderItem"/> 一对一（主键同明细 Id）。
    /// 记录执行过程中的利润相关汇总（票/款）与进度：采购、入库、付款、进项开票（各维度含状态 + 数量或金额口径）。
    /// </summary>
    [Table("purchaseorderitemextend")]
    public class PurchaseOrderItemExtend : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("PurchaseOrderItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>剩余可下达货通知数量（文档 QtyStockInNotifyNot，由业务重算）</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockInNotifyNot { get; set; }

        /// <summary>累计预期到货（所有批次 ExpectQty 之和）</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockInNotifyExpectSum { get; set; }

        /// <summary>
        /// 累计入库数量：所有「已入库」的采购入库单（StockInType=采购）中，头表 <c>PurchaseOrderItemId</c> 等于本行主键时，按单头 <c>TotalQuantity</c> 累计。
        /// （与到货通知 ReceiveQty 独立，以入库单为有效入库凭证。）
        /// </summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyReceiveTotal { get; set; }

        // --- 票（进项）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceDone { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceToBe { get; set; }

        // --- 款（采购付款）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountFinish { get; set; }

        /// <summary>
        /// 累计请款金额：有效付款单明细请款额 PaymentAmountToBe 之和（含待审核、未完成核销），与 PaymentAmountFinish（已核销）区分。
        /// </summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountRequested { get; set; }

        // --- 关联销售侧收款汇总（以销定采对账用，可选维护）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountFinish { get; set; }

        /// <summary>采购执行进度：0=待采购 1=部分采购 2=采购完成（本明细状态；主单或明细取消时数量记 0、状态待采购）</summary>
        public short PurchaseProgressStatus { get; set; }

        /// <summary>本行有效采购数量：明细未取消且采购主单未取消时为行 Qty，否则 0。</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal PurchaseProgressQty { get; set; }

        /// <summary>入库进度：0=待入库 1=部分入库 2=入库完成（相对本行有效采购数量与 <see cref="QtyReceiveTotal"/>）</summary>
        public short StockInProgressStatus { get; set; }

        /// <summary>付款进度：0=待付款 1=部分付款 2=付款完成（已核销金额相对应付口径）</summary>
        public short PaymentProgressStatus { get; set; }

        /// <summary>进项开票进度：0=待开票 1=部分开票 2=开票完成（已开票金额相对应付票额）</summary>
        public short InvoiceProgressStatus { get; set; }
    }
}

