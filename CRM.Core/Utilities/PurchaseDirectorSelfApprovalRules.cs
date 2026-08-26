using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 采购总监可审批本人提交的供应商、采购订单。经理/员工仍禁止自审；销售总监不适用。
/// </summary>
public static class PurchaseDirectorSelfApprovalRules
{
    /// <summary>系统管理员或采购/采购运营部总监：可对本人提交的供应商 / 采购订单做通过或拒绝。</summary>
    public static bool AllowsOwnVendorOrPurchaseOrderDecide(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.IsSysAdmin)
            return true;
        return RfqItemQuoteAccessRules.IsPurchaseDepartmentDirector(summary);
    }
}
