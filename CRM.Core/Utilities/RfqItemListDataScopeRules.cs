using CRM.Core.Interfaces;

namespace CRM.Core.Utilities;

/// <summary>
/// 需求明细行是否按销售/采购数据范围裁行。
/// 用于需求明细作业页（含看板）与报价列表看板并联需求；
/// 与需求主表 <c>ApplyRfqMainListDataScopeAsync</c>、报价列表 <c>ApplyQuoteListDataScopeAsync</c> 对齐：管理角色 <c>HasBizDataBypass</c> 不裁行。
/// </summary>
public static class RfqItemListDataScopeRules
{
    /// <summary>
    /// 为 true 时应对需求明细行做销售/采购范围过滤。
    /// SuperAdmin / Admin / Manager 的 <c>HasBizDataBypass</c> 为 true，不得只认 <c>IsSysAdmin</c>。
    /// 销售或采购任一侧范围为「全部」(0) 时亦不裁行（与主表一致）。
    /// </summary>
    public static bool ShouldApplyJobPageScope(UserPermissionSummaryDto? summary)
    {
        if (summary == null)
            return false;
        if (summary.HasBizDataBypass)
            return false;
        return summary.SaleDataScope != 0 && summary.PurchaseDataScope != 0;
    }
}
