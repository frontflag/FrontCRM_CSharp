namespace CRM.Core.Interfaces;

/// <summary>
/// 提交审核后预热审批桌面所需的客户/供应商调查：无最新报告时后台自动调查（不阻塞提交响应）。
/// </summary>
public interface IApprovalPartyIntelWarmupService
{
    /// <param name="bizType">CUSTOMER / VENDOR / SALES_ORDER / PURCHASE_ORDER / FINANCE_RECEIPT / FINANCE_PAYMENT</param>
    /// <param name="businessId">业务主键</param>
    /// <param name="userId">触发人（可空）</param>
    void ScheduleAfterSubmit(string bizType, string businessId, string? userId);
}
