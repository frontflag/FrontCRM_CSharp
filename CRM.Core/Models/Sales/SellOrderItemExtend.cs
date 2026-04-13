using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Constants;
using CRM.Core.Models;

namespace CRM.Core.Models.Sales
{
    /// <summary>
    /// 销售明细扩展：货/票/款汇总及出货通知数量（与 sellorderitem 一对一，主键同明细 Id）
    /// </summary>
    [Table("sellorderitemextend")]
    public class SellOrderItemExtend : BaseGuidEntity
    {
        [Key]
        [StringLength(36)]
        [Column("SellOrderItemId")]
        public override string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>已关联采购订单的采购数量合计（所有 PO 明细行 Qty 之和）。</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyAlreadyPurchased { get; set; }

        /// <summary>未采购数量（销售数量 − 已采购数量，不小于 0）。</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyNotPurchase { get; set; }

        /// <summary>已下达出库通知数量累计</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockOutNotify { get; set; }

        /// <summary>剩余可下达出库通知数量</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockOutNotifyNot { get; set; }

        /// <summary>已实际出库数量：已完成的销售出库单（StockOutType=销售）头表 <c>SellOrderItemId</c> 等于本行时，按单头 <c>TotalQuantity</c> 累计。</summary>
        [Column(TypeName = "numeric(18,4)")]
        public decimal QtyStockOutActual { get; set; }

        // --- 销项发票 ---
        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal InvoiceAmountFinish { get; set; }

        /// <summary>采购执行进度：0=待采购 1=部分采购 2=采购完成（相对销售数量）</summary>
        public short PurchaseProgressStatus { get; set; }

        /// <summary>入库进度（采购入库单头 <c>SellOrderItemId</c> 等于本行时按 <c>TotalQuantity</c> 与销售数量比）：0=待入库 1=部分入库 2=入库完成</summary>
        public short StockInProgressStatus { get; set; }

        /// <summary>出库进度：0=待出库 1=部分出库 2=出库完成</summary>
        public short StockOutProgressStatus { get; set; }

        /// <summary>
        /// 同 PN+品牌下所有备货库存（<c>StockType</c>=备货）在库可用量 <c>QtyRepertoryAvailable</c> 之和（截断为非负 int；不与销售数量比较；同键多销售行共享同一池合计）。
        /// 刷新：采购备货入库单状态为已完成(2)过账后；销售出库执行扣减备货库存后；销售订单明细新建或整单替换明细后。
        /// 前端：API 字段 <c>purchasedStockAvailableQty</c> 大于 0 时在申请出库场景放宽「须先满足采购门槛」的限制（见 <c>salesOrderLinePurchasedStockReliefOk</c>）。
        /// </summary>
        [Column("PurchasedStock_AvailableQty")]
        public int PurchasedStock_AvailableQty { get; set; }

        /// <summary>收款进度：0=待收款 1=部分收款 2=收款完成</summary>
        public short ReceiptProgressStatus { get; set; }

        /// <summary>销项开票进度：0=待开票 1=部分开票 2=开票完成</summary>
        public short InvoiceProgressStatus { get; set; }

        // --- 收款 ---
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountNot { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ReceiptAmountFinish { get; set; }

        // --- 付款（采购侧，对销售明细维度汇总时可维护）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountDone { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PaymentAmountToBe { get; set; }

        // --- 进项发票（镜像）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseInvoiceDone { get; set; }

        // --- P1：报价快照与汇率快照（下单时固化）---
        [StringLength(36)]
        public string? QuoteItemId { get; set; }

        /// <summary>报价单价（原币，来自报价明细 UnitPrice）</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal QuoteCost { get; set; }

        /// <summary>报价币别（与 <see cref="CurrencyCode"/> 一致）</summary>
        public short QuoteCurrency { get; set; }

        /// <summary>报价单价折合 USD（下单时汇率）</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal QuoteConvertCost { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal FxUsdToCnySnapshot { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal FxUsdToHkdSnapshot { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal FxUsdToEurSnapshot { get; set; }

        /// <summary>销售单价折合 USD（下单快照）</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal SellConvertUsdUnitSnapshot { get; set; }

        /// <summary>销售金额 USD 快照（Qty×SellConvertUsdUnitSnapshot）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal SellLineAmountUsdSnapshot { get; set; }

        /// <summary>P1：报价利润（USD，基于下单时销售/报价折算快照）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal QuoteProfitExpected { get; set; }

        /// <summary>报价毛利率口径：销售收入USD / 报价成本USD（快照）；成本为 0 时为 0</summary>
        [Column(TypeName = "numeric(18,6)")]
        public decimal QuoteProfitRateExpected { get; set; }

        /// <summary>按当前销售单价折算 USD 重算的报价利润（原报价成本快照不变）</summary>
        [Column(TypeName = "numeric(18,2)")]
        public decimal ReQuoteProfitExpected { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal ReQuoteProfitRateExpected { get; set; }

        // --- P1：采购利润（USD，随 PO 汇总变化）---
        [Column(TypeName = "numeric(18,2)")]
        public decimal PoCostUsdTotal { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal PurchaseProfitExpected { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal PurchaseProfitRateExpected { get; set; }

        // --- P2：仅统计采购明细 Status≥已确认(30) 的成本；预计销售利润 = 当前销售折 USD 总额 − 已确认采购折 USD --- 
        [Column(TypeName = "numeric(18,2)")]
        public decimal PoCostUsdConfirmed { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal SalesProfitExpected { get; set; }

        // --- P3：出库利润（业务 USD：均价采购折 USD vs 销售折 USD×实出数量）；财务 USD 暂与业务同口径，待绑定出库时点汇率/成本方案 ---
        [Column(TypeName = "numeric(18,2)")]
        public decimal ProfitOutBizUsd { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal ProfitOutRateBiz { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal ProfitOutFinUsd { get; set; }

        [Column(TypeName = "numeric(18,6)")]
        public decimal ProfitOutRateFin { get; set; }
    }
}
