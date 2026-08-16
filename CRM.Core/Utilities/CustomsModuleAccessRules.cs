using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>报关板块模块级准入（与 RBAC PRD §5.5.1 一致）：物流、财务、系统/平台管理员。</summary>
public static class CustomsModuleAccessRules
{
    /// <summary>与 <see cref="RbacDepartment.IdentityType"/> 约定：5=Finance。</summary>
    public const short FinanceIdentityType = 5;

    /// <summary>与 <see cref="RbacDepartment.IdentityType"/> 约定：6=Logistics。</summary>
    public const short LogisticsIdentityType = 6;

    public static bool CanAccessModule(UserPermissionSummaryDto summary)
    {
        if (summary.IsSysAdmin || summary.IsSysManager || summary.HasBizDataBypass)
            return true;
        return summary.IdentityType is FinanceIdentityType or LogisticsIdentityType;
    }

    /// <summary>报关单/明细列表：财务与管理员看全量；物流部仍按 <c>LogisticsDataScope</c> 收窄创建人。</summary>
    public static bool BypassLogisticsDataScopeForCustomsList(UserPermissionSummaryDto summary) =>
        summary.IsSysAdmin
        || summary.IsSysManager
        || summary.HasBizDataBypass
        || summary.IdentityType == FinanceIdentityType;
}
