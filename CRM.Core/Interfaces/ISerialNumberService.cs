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
        /// <returns>格式化后的完整编号（如 CUS00000、SOUT001ZB）</returns>
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
        public const string Customer = "Customer";           // 客户：CUS + 5位32进制流水号
        public const string Vendor = "Vendor";               // 供应商：VEN + 5位32进制流水号
        public const string RFQ = "RFQ";                     // 询价/需求：RFQ + 5位32进制流水号
        public const string Quotation = "Quotation";         // 报价：QUO + 5位32进制流水号
        public const string SalesOrder = "SalesOrder";       // 销售订单：SOR + 5位32进制流水号
        public const string PurchaseOrder = "PurchaseOrder"; // 采购订单：PUR + 5位32进制流水号
        public const string PurchaseRequisition = "PurchaseRequisition"; // 采购申请：PRQ + 5位32进制流水号
        public const string StockIn = "StockIn";             // 入库：STI + 5位32进制流水号
        public const string StockOut = "StockOut";           // 出库执行：SOUT + 5位32进制流水号
        public const string StockOutRequest = "StockOutRequest"; // 出库申请：SORQ + 5位32进制流水号
        public const string PickingTask = "PickingTask";     // 拣货任务：PKT + 5位32进制流水号
        public const string ArrivalNotice = "ArrivalNotice"; // 到货通知：ARN + 5位32进制流水号
        public const string QcRecord = "QcRecord";           // 质检单：QCR + 5位32进制流水号
        public const string PaymentRequest = "PaymentRequest"; // 请款单：PMR + 5位32进制流水号
        public const string FinancePayment = "FinancePayment"; // 财务付款单：FNP + 5位32进制流水号
        public const string Stock = "Stock";                 // 库存：STK + 5位32进制流水号
        public const string Receipt = "Receipt";             // 收款：REC + 5位32进制流水号
        public const string Payment = "Payment";             // 付款：PAY + 5位32进制流水号
        public const string InputInvoice = "InputInvoice";   // 进项发票：INVI + 5位32进制流水号
        public const string OutputInvoice = "OutputInvoice"; // 销项发票：OUTI + 5位32进制流水号
    }
}
