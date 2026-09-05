using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 商务部商务助理（IdentityType=4）：主部门 <c>FinanceDataScope=4</c> 时仍可见收款侧菜单与列表，
/// 数据范围与销售订单一致（映射业务员 <c>SalesUserId ∈ M</c> 或跟单 <c>Assistor == 自己</c>）。
/// </summary>
public static class CommerceAssistantFinanceAccessRules
{
    /// <summary>收款侧菜单：商务助理在非 bypass 时仍显示财务区（仅收款组，不含付款组）。</summary>
    public static bool CanAccessReceiptSideMenus(UserPermissionSummaryDto? summary) =>
        summary != null
        && BusinessDepartmentRules.UseCommerceAssistantMappedSalespersonScope(summary);

    /// <summary>跳过财务部「无数据」(FinanceDataScope=4) 对收款侧列表/详情的拦截。</summary>
    public static bool ShouldBypassFinanceDataScopeDenial(UserPermissionSummaryDto? summary) =>
        CanAccessReceiptSideMenus(summary);
}
