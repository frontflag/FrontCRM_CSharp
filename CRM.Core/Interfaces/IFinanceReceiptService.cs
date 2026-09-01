using CRM.Core.Constants;
using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 收款服务接口
    /// </summary>
    public interface IFinanceReceiptService
    {
        Task<FinanceReceipt> CreateAsync(CreateFinanceReceiptRequest request, string? actingUserId = null);
        Task<FinanceReceipt?> GetByIdAsync(string id);
        Task<IEnumerable<FinanceReceipt>> GetAllAsync();
        /// <summary>
        /// 仅新建可编辑。传入 <c>ReceiptAmount</c> 时同步唯一未核销、未转预收的默认明细
        /// （<c>ReceiptAmount</c> / <c>ReceiptConvertAmount</c>）；无明细则补一条；多明细不改。
        /// </summary>
        Task<FinanceReceipt> UpdateAsync(string id, UpdateFinanceReceiptRequest request, string? actingUserId = null);
        /// <param name="actingUserId">当前登录用户 ID（写入 log_operation 删除人）</param>
        Task DeleteAsync(string id, string? actingUserId = null);
        Task ForceDeleteAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName);
        /// <summary>反核销：撤销本单全部核销（主单状态不变）；须无预收池入账。</summary>
        Task<FinanceReceipt> ReverseVerificationAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName);
        /// <summary>确认收款单（新建 → 确认），写操作日志。</summary>
        Task ConfirmAsync(string id, string? actingUserId = null, string? actingUserName = null);
        Task UpdateStatusAsync(string id, short status, string? actingUserId = null, string? actingUserName = null);
        Task VerifyReceiptItemAsync(string receiptItemId, string sellInvoiceId, decimal amount, string? actingUserId = null);
        Task<PagedResult<FinanceReceipt>> GetPagedAsync(FinanceReceiptQueryRequest request);
    }

    public class CreateFinanceReceiptRequest
    {
        public string FinanceReceiptCode { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? SalesUserId { get; set; }
        public decimal ReceiptAmount { get; set; }
        public byte ReceiptCurrency { get; set; } = 1;
        public DateTime? ReceiptDate { get; set; }
        public string? ReceiptUserId { get; set; }
        public short ReceiptMode { get; set; } = 1;
        public string? ReceiptBankId { get; set; }
        public string? BankSlipNo { get; set; }
        public string? Remark { get; set; }
        /// <summary>收款单级预收标识（明细为空时用于自动生成明细行）</summary>
        public short ReceiptPurpose { get; set; } = FinanceReceiptPurposeCode.Normal;
        /// <summary>预收可选挂销售订单</summary>
        public string? AdvanceSellOrderId { get; set; }
        public bool IsFreightForwarderPayment { get; set; }
        public string? FreightForwarderCompanyId { get; set; }
        public List<CreateFinanceReceiptItemRequest> Items { get; set; } = new();
    }

    public class CreateFinanceReceiptItemRequest
    {
        public string? SellOrderId { get; set; }
        public string? SellOrderItemId { get; set; }
        public string? FinanceSellInvoiceId { get; set; }
        public string? FinanceSellInvoiceItemId { get; set; }
        public decimal ReceiptAmount { get; set; }
        public string? StockOutItemId { get; set; }
        public string? ProductId { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        public short ReceiptPurpose { get; set; } = FinanceReceiptPurposeCode.Normal;
        public string? AdvanceSellOrderId { get; set; }
        public string? Remark { get; set; }
    }

    public class UpdateFinanceReceiptRequest
    {
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        /// <summary>单头收款金额。传入时后端同步默认明细（见 <see cref="IFinanceReceiptService.UpdateAsync"/>）。</summary>
        public decimal? ReceiptAmount { get; set; }
        public byte? ReceiptCurrency { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public short? ReceiptMode { get; set; }
        public string? BankSlipNo { get; set; }
        public string? Remark { get; set; }
        public bool? IsFreightForwarderPayment { get; set; }
        public string? FreightForwarderCompanyId { get; set; }
    }

    public class FinanceReceiptQueryRequest
    {
        public string? Keyword { get; set; }
        public short? Status { get; set; }
        /// <summary>收款用途：10 普通 / 20 预收（筛主表 ReceiptPurpose）。</summary>
        public short? ReceiptPurpose { get; set; }
        /// <summary>整单核销状态：0未核销 / 1部分核销 / 2核销完成（按明细 VerificationStatus 汇总）。</summary>
        public short? VerificationStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        /// <summary>收款单头币别精确匹配（finance_receipt.receipt_currency）。财务分析原币下钻用，列表筛选项不展示。</summary>
        public short? ReceiptCurrency { get; set; }
        /// <summary>收款日期起（含当日，筛 ReceiptDate；与列表创建时间 startDate 独立）。</summary>
        public DateTime? ReceiptDateFrom { get; set; }
        /// <summary>收款日期止（含当日，筛 ReceiptDate）。</summary>
        public DateTime? ReceiptDateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }
}
