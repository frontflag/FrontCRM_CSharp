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
        /// <returns>格式化后的完整编号（如 Cus0001）</returns>
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
        public const string Customer = "Customer";           // 客户：CUS0001
        public const string Vendor = "Vendor";               // 供应商：VEN0001
        public const string RFQ = "RFQ";                     // 询价/需求：RFQ0001
        public const string Quotation = "Quotation";         // 报价：QUO0001
        public const string SalesOrder = "SalesOrder";       // 销售订单：SO0001
        public const string PurchaseOrder = "PurchaseOrder"; // 采购订单：PO0001
        public const string StockIn = "StockIn";             // 入库：STI0001
        public const string StockOut = "StockOut";           // 出库：STO0001
        public const string Stock = "Stock";                 // 库存：STK0001
        public const string Receipt = "Receipt";             // 收款：REC0001
        public const string Payment = "Payment";             // 付款：PAY0001
        public const string InputInvoice = "InputInvoice";   // 进项发票：INVI0001
        public const string OutputInvoice = "OutputInvoice"; // 销项发票：INVO0001
    }
}
