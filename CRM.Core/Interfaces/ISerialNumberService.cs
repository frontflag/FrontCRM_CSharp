namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 系统流水号服务接口
    /// 负责为各业务模块生成唯一、连续的编号
    /// </summary>
    public interface ISerialNumberService
    {
        /// <summary>
        /// 生成下一个流水号（线程安全，使用数据库行锁）
        /// </summary>
        /// <param name="moduleCode">业务模块代码</param>
        /// <returns>格式化后的完整编号（前缀 + 5 位 32 进制，如 CUS00000、PAY00000）</returns>
        Task<string> GenerateNextAsync(string moduleCode);

        /// <summary>
        /// 预览下一个流水号（不消耗序号）
        /// </summary>
        Task<string> PreviewNextAsync(string moduleCode);

        /// <summary>
        /// 获取指定模块的当前序号
        /// </summary>
        Task<int> GetCurrentSequenceAsync(string moduleCode);

        /// <summary>
        /// 重置指定模块的流水号（谨慎使用）
        /// </summary>
        Task ResetSequenceAsync(string moduleCode, int startFrom = 0);
    }

    /// <summary>
    /// 系统预定义的业务模块代码常量
    /// </summary>
    public static class ModuleCodes
    {
        public const string Customer = "Customer";           // 客户 CUS
        public const string Vendor = "Vendor";               // 供应商 VEN
        public const string RFQ = "RFQ";                     // 询价/需求 RFQ
        public const string Quotation = "Quotation";         // 报价 QUO
        public const string SalesOrder = "SalesOrder";       // 销售订单 SO
        public const string PurchaseOrder = "PurchaseOrder"; // 采购订单 PO
        public const string StockIn = "StockIn";             // 入库 STI
        public const string StockOut = "StockOut";           // 出库 STO
        public const string Receipt = "Receipt";             // 收款 REC
        public const string Payment = "Payment";             // 付款 PAY_DEL（遗留付款单模块）
        public const string InputInvoice = "InputInvoice";   // 进项发票 INVI
        public const string OutputInvoice = "OutputInvoice"; // 销项发票 INVO
        public const string Stock = "Stock";                 // 库存 STK
        public const string PurchaseRequisition = "PurchaseRequisition"; // 采购申请 POR
        public const string StockOutRequest = "StockOutRequest"; // 出库申请 STOR
        public const string PickingTask = "PickingTask";     // 拣货任务 PAK
        public const string ArrivalNotice = "ArrivalNotice"; // 到货通知 STIR
        public const string QcRecord = "QcRecord";           // 质检 QC
        public const string PaymentRequest = "PaymentRequest"; // 请款 PAYR
        public const string FinancePayment = "FinancePayment"; // 财务付款 PAY
        /// <summary>移库单 STF（报关迁库等）。</summary>
        public const string StockTransfer = "StockTransfer";

        /// <summary>手工移库单 STM。</summary>
        public const string StockTransferManual = "StockTransferManual";
        /// <summary>报关单 CDS。</summary>
        public const string CustomsDeclaration = "CustomsDeclaration";
        /// <summary>报关公司 CBR。</summary>
        public const string CustomsBroker = "CustomsBroker";
        // 以上前缀 + 5 位 32 进制数值位，由 SerialNumberService 拼接
    }
}
