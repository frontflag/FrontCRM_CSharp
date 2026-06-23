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
    /// 商务部销售助理：主部门 <see cref="UserPermissionSummaryDto.SaleDataScope"/> = 4 时，仅可见 <c>assistor</c> 为自己的销售订单（与采购助理 + PurchaseDataScope=4 对称）。
    /// </summary>
    public static bool UseSellOrderAssistorOnlyScope(UserPermissionSummaryDto summary) =>
        summary.IdentityType == 4 && summary.SaleDataScope == 4;

    /// <summary>
    /// 采购助理跟单：<c>PurchaseDataScope = 4</c> 时，仅可见 <c>assistor</c> 为自己的采购订单（与 <see cref="DataPermissionService.ApplyPurchaseOrderDataScopeAsync"/> 一致）。
    /// </summary>
    public static bool UsePurchaseOrderAssistorOnlyScope(UserPermissionSummaryDto summary) =>
        summary.PurchaseDataScope == 4;
}
