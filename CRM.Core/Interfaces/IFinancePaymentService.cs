using CRM.Core.Models.Finance;

namespace CRM.Core.Interfaces
{
    /// <summary>
    /// 付款服务接口
    /// </summary>
    public interface IFinancePaymentService
    {
        Task<FinancePayment> CreateAsync(CreateFinancePaymentRequest request, string? actingUserId = null);
        Task<FinancePayment?> GetByIdAsync(string id);
        Task<IEnumerable<FinancePayment>> GetAllAsync();
        Task<FinancePayment> UpdateAsync(string id, UpdateFinancePaymentRequest request, string? actingUserId = null);
        /// <summary>编辑请款（仅 status 1 / -1；-1 保存后转为 1）。</summary>
        Task<FinancePayment> UpdateRequestAsync(string id, UpdateFinancePaymentRequestBody request, string? actingUserId = null);
        /// <summary>保存付款执行信息（仅 status 10）。</summary>
        Task<FinancePayment> UpdateExecutionAsync(string id, UpdateFinancePaymentExecutionRequest request, string? actingUserId = null);
        /// <summary>撤回审核通过的请款（10→1），清空执行侧字段并删除水单附件。</summary>
        Task<FinancePayment> WithdrawAsync(string id, string actingUserId, bool actingUserHasFinancePaymentWrite);
        /// <param name="actingUserId">当前登录用户 ID（写入 log_operation 删除人）</param>
        Task DeleteAsync(string id, string? actingUserId = null);
        /// <summary>管理员强制删除：确认单号、守卫、删除（含采购扩展回算）并写操作日志。</summary>
        Task ForceDeleteAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName);
        /// <summary>反核销：仅 status=100；回滚明细核销并 100→10，重算 PO 付款状态。</summary>
        Task<FinancePayment> ReverseVerificationAsync(string id, string confirmBillCode, string actingUserId, string? actingUserName);
        /// <param name="remark">审核驳回原因等补充说明（可选）</param>
        Task UpdateStatusAsync(string id, short status, string? remark = null, string? actingUserId = null);
        Task VerifyPaymentItemAsync(string paymentItemId, decimal amount);
        Task<PagedResult<FinancePayment>> GetPagedAsync(FinancePaymentQueryRequest request);
    }

    public class CreateFinancePaymentRequest
    {
        public string FinancePaymentCode { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string? VendorName { get; set; }
        public decimal PaymentAmountToBe { get; set; }
        public byte PaymentCurrency { get; set; } = 1;
        public DateTime? PaymentDate { get; set; }
        public string? PaymentUserId { get; set; }
        public short PaymentMode { get; set; } = 1;
        public string? BankSlipNo { get; set; }
        /// <summary>财务参数-付款银行主键；请款时由服务端按 VendorBankId 推导。</summary>
        public string? FinancePaymentBankId { get; set; }
        /// <summary>供应商银行账户 ID（vendorbankinfo.BankId）；请款时建议必填。</summary>
        public string? VendorBankId { get; set; }
        /// <summary>请款人申请备注。</summary>
        public string? RequestRemark { get; set; }
        public decimal FeeIntermediateBank { get; set; }
        public decimal FeeBankCharge { get; set; }
        public decimal FeeFreight { get; set; }
        public decimal FeeMisc { get; set; }
        public decimal FeeRounding { get; set; }
        /// <summary>我方 / 供应商</summary>
        public string? FeeIntermediateBankPayer { get; set; }
        /// <summary>通用备注（财务手工建单等）。</summary>
        public string? Remark { get; set; }
        public List<CreateFinancePaymentItemRequest> Items { get; set; } = new();
    }

    public class CreateFinancePaymentItemRequest
    {
        public string? PurchaseOrderId { get; set; }
        public string? PurchaseOrderItemId { get; set; }
        public decimal PaymentAmountToBe { get; set; }
        public string? ProductId { get; set; }
        public string? PN { get; set; }
        public string? Brand { get; set; }
        public string? LineRemark { get; set; }
    }

    public class UpdateFinancePaymentRequestBody
    {
        public string? VendorBankId { get; set; }
        public short PaymentMode { get; set; } = 1;
        public byte PaymentCurrency { get; set; } = 1;
        public string? RequestRemark { get; set; }
        public decimal FeeIntermediateBank { get; set; }
        public decimal FeeBankCharge { get; set; }
        public decimal FeeFreight { get; set; }
        public decimal FeeMisc { get; set; }
        public decimal FeeRounding { get; set; }
        public string? FeeIntermediateBankPayer { get; set; }
        public List<UpdateFinancePaymentItemRequest> Items { get; set; } = new();
    }

    public class UpdateFinancePaymentItemRequest
    {
        public string Id { get; set; } = string.Empty;
        public decimal PaymentAmountToBe { get; set; }
        public string? LineRemark { get; set; }
    }

    public class UpdateFinancePaymentExecutionRequest
    {
        public string? CompanyBankId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? BankSlipNo { get; set; }
        /// <summary>付款执行阶段可调整的费用明细（仅 status 10；不改明细行请款额）。</summary>
        public decimal FeeIntermediateBank { get; set; }
        public decimal FeeBankCharge { get; set; }
        public decimal FeeFreight { get; set; }
        public decimal FeeMisc { get; set; }
        public decimal FeeRounding { get; set; }
        public string? FeeIntermediateBankPayer { get; set; }
    }

    public class UpdateFinancePaymentRequest
    {
        public decimal? PaymentAmountToBe { get; set; }
        /// <summary>付款币别 1:人民币 2:美元 3:欧元</summary>
        public byte? PaymentCurrency { get; set; }
        public DateTime? PaymentDate { get; set; }
        public short? PaymentMode { get; set; }
        public string? BankSlipNo { get; set; }
        public string? FinancePaymentBankId { get; set; }
        /// <summary>公司付款银行账户主键（company_bankinfo.Id）。</summary>
        public string? CompanyBankId { get; set; }
        public string? VendorBankId { get; set; }
        public string? RequestRemark { get; set; }
        public decimal? FeeIntermediateBank { get; set; }
        public decimal? FeeBankCharge { get; set; }
        public decimal? FeeFreight { get; set; }
        public decimal? FeeMisc { get; set; }
        public decimal? FeeRounding { get; set; }
        public string? FeeIntermediateBankPayer { get; set; }
        public string? Remark { get; set; }
    }

    public class FinancePaymentQueryRequest
    {
        /// <summary>兼容旧版：付款单号或供应商模糊检索。</summary>
        public string? Keyword { get; set; }
        public string? FinancePaymentCode { get; set; }
        public string? FreightForwarderOrderNo { get; set; }
        public string? BankSlipNo { get; set; }
        public short? PaymentMode { get; set; }
        public string? VendorName { get; set; }
        /// <summary>关联采购订单号模糊（任一明细命中即保留）。</summary>
        public string? PurchaseOrderCode { get; set; }
        /// <summary>关联采购订单采购员姓名模糊（任一明细命中即保留）。</summary>
        public string? PurchaseUserName { get; set; }
        /// <summary>关联采购订单币种精确匹配（purchaseorder.Currency）。</summary>
        public short? PurchaseCurrency { get; set; }
        public string? Remark { get; set; }
        public short? Status { get; set; }
        /// <summary>付款日期起（含）。</summary>
        public DateTime? StartDate { get; set; }
        /// <summary>付款日期止（含当日时按 +1 天边界处理）。</summary>
        public DateTime? EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? CurrentUserId { get; set; }
    }
}
