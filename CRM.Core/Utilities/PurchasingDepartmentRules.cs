using CRM.Core.Models.Rbac;

namespace CRM.Core.Utilities
{
    /// <summary>
    /// 采购相关部门与「询价/需求采购员」业务范围划分：
    /// 采购部等业务部门参与分配；采购运营部仅内部运营，不参与需求采购员轮询与采购员树。
    /// </summary>
    public static class PurchasingDepartmentRules
    {
        /// <summary>采购运营部（名称匹配，与 sys_department.DepartmentName 一致）。</summary>
        public static bool IsPurchasingOperationsDepartment(RbacDepartment d)
        {
            var n = d.DepartmentName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(n)) return false;
            return n.Contains("采购运营", StringComparison.OrdinalIgnoreCase)
                   || n.Contains("purchasing operations", StringComparison.OrdinalIgnoreCase)
                   || n.Contains("purchase operations", StringComparison.OrdinalIgnoreCase)
                   || n.Contains("procurement operations", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 参与询价采购员分配 / 采购员下拉的采购相关部门：采购或采购助理身份，或名称含采购/purchase，
        /// 但排除采购运营部（即使其 IdentityType 被标为采购类）。
        /// </summary>
        public static bool IsPurchaseDepartmentForRfqBuyer(RbacDepartment d)
        {
            if (d.Status != 1) return false;
            if (IsPurchasingOperationsDepartment(d)) return false;
            if (d.IdentityType is 2 or 3) return true;
            var name = d.DepartmentName ?? string.Empty;
            return name.Contains("采购", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("purchase", StringComparison.OrdinalIgnoreCase);
        }
    }
}
