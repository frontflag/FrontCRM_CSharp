using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Finance;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Analytics;

/// <summary>
/// 销售分析待办「待核销应收款」：与应收款列表看板「待核销应收款」同源
///（Σ verified_to_be；美金优先 SO 行 convert_price/price，否则查询日财务汇率）。
/// </summary>
internal static class SalesAnalyticsTodoReceivable
{
    public static async Task<SalesAnalyticsMoneyDto> BuildAsync(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IFinanceExchangeRateService exchangeRateService,
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        if (scope.MaskAmounts)
            return SalesAnalyticsTodoMoney.Empty(masked: true);

        var userId = scope.Summary.UserId;
        // 与列表看板「待核销应收款」一致：Σ verified_to_be（仅待核销过滤不改变合计）
        var q = db.FinanceReceivables.AsNoTracking()
            .Where(r => !r.IsDeleted && r.VerifiedToBe > 0m);
        q = await dataPermission.ApplyFinanceReceivableListDataScopeAsync(userId, q, cancellationToken);
        q = ApplyViewLens(db, q, scope);

        var rows = await (
            from r in q
            join oi in db.SellOrderItems.AsNoTracking() on r.SellOrderItemId equals oi.Id into oiJoin
            from oi in oiJoin.DefaultIfEmpty()
            select new
            {
                r.Currency,
                r.VerifiedToBe,
                Price = oi != null ? oi.Price : 0m,
                ConvertPrice = oi != null ? oi.ConvertPrice : 0m
            }).ToListAsync(cancellationToken);

        var rates = await exchangeRateService.GetCurrentAsync(cancellationToken);
        return SalesAnalyticsTodoMoney.Build(
            rows.Select(r => (r.VerifiedToBe, r.Currency, r.Price, r.ConvertPrice)),
            rates,
            maskAmounts: false);
    }

    private static IQueryable<FinanceReceivable> ApplyViewLens(
        ApplicationDbContext db,
        IQueryable<FinanceReceivable> q,
        SalesAnalyticsResolvedScope scope)
    {
        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && !BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary)
            && !string.IsNullOrWhiteSpace(scope.SalesUserId)
            && scope.Summary.SaleDataScope != 1)
        {
            q = q.Where(r => r.SalesUserId == scope.SalesUserId);
        }

        if (scope.ViewLevel != SalesAnalyticsViewLevels.Department)
            return q;

        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(r => r.SalesUserId == null || !withPrimary.Contains(r.SalesUserId));
        }

        var userIdsInDept = db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);
        return q.Where(r => r.SalesUserId != null && userIdsInDept.Contains(r.SalesUserId));
    }
}
