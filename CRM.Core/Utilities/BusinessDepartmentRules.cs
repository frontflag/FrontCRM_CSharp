using CRM.Core.Interfaces;
using CRM.Core.Models.Rbac;

namespace CRM.Core.Utilities;

/// <summary>
/// 商务相关部门（IdentityType=4）与「销售助理跟单」数据范围判定。
/// </summary>
public static class BusinessDepartmentRules
{
    /// <summary>商务部相关部门：身份为商务(4)，或部门名称含商务/business。</summary>
    public static bool IsBusinessDepartment(RbacDepartment d)
    {
        if (d.Status != 1) return false;
        if (d.IdentityType == 4) return true;
        var n = d.DepartmentName ?? string.Empty;
        return n.Contains("商务", StringComparison.OrdinalIgnoreCase)
               || n.Contains("business", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 商务部（IdentityType=4）且非业务 bypass：销售侧数据范围由 <c>sys_relation_map</c> type=100 映射业务员决定，忽略主部门 SaleDataScope。
    /// </summary>
    public static bool UseCommerceAssistantMappedSalespersonScope(UserPermissionSummaryDto summary) =>
        summary.IdentityType == 4 && !summary.HasBizDataBypass;

    /// <summary>
    /// 历史命名：销售看板「跟单专属」个人层。与 <see cref="UseCommerceAssistantMappedSalespersonScope"/> 对齐（不再仅限 SaleDataScope=4）。
    /// </summary>
    public static bool UseSellOrderAssistorOnlyScope(UserPermissionSummaryDto summary) =>
        UseCommerceAssistantMappedSalespersonScope(summary);

    /// <summary>
    /// 采购助理跟单：<c>PurchaseDataScope = 4</c> 时，仅可见 <c>assistor</c> 为自己的采购订单（与 <see cref="DataPermissionService.ApplyPurchaseOrderDataScopeAsync"/> 一致）。
    /// </summary>
    public static bool UsePurchaseOrderAssistorOnlyScope(UserPermissionSummaryDto summary) =>
        summary.PurchaseDataScope == 4;
}
