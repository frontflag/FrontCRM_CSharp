using CRM.Core.Interfaces;
using CRM.Core.Models.Analytics;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Analytics;

/// <summary>
/// 销售订单列表同等数据范围下的基线聚合（对账用第二套实现，结构与 <see cref="SalesOrderListQuery"/> 对齐）。
/// </summary>
public sealed class SalesAnalyticsListBaselineQuery : ISalesAnalyticsReconciliationBaseline
{
    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public SalesAnalyticsListBaselineQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    public async Task<SalesAnalyticsSnapshotDto> GetSnapshotAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var userId = scope.Summary.UserId;
        var orders = await BuildScopedOrdersAsync(userId, scope, cancellationToken);
        orders = SalesAnalyticsDateFilter.ApplyCreateTimeRange(orders, scope.DateFrom, scope.DateTo);

        if (BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary))
        {
            return await BuildOrderOnlySnapshotAsync(orders, scope.MaskAmounts, cancellationToken);
        }

        var rfqJoin = await BuildScopedRfqItemJoinAsync(userId, scope, cancellationToken);
        var dateFrom = SalesAnalyticsDateFilter.ToUtcDateStart(scope.DateFrom);
        var dateEnd = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(scope.DateTo);
        var rfqInPeriod = rfqJoin.Where(x =>
            x.Item.CreateTime >= dateFrom && x.Item.CreateTime < dateEnd);

        var rfqItemCount = await rfqInPeriod.CountAsync(cancellationToken);
        var rfqCustomerCount = await rfqInPeriod
            .Where(x => x.Rfq.CustomerId != null)
            .Select(x => x.Rfq.CustomerId!)
            .Distinct()
            .CountAsync(cancellationToken);

        var rfqItemIds = await rfqInPeriod.Select(x => x.Item.Id).ToListAsync(cancellationToken);
        var convertedCount = 0;
        if (rfqItemIds.Count > 0)
        {
            convertedCount = await (
                from oi in _db.SellOrderItems.AsNoTracking()
                join q in _db.Quotes.AsNoTracking() on oi.QuoteId equals q.Id
                where oi.QuoteId != null && q.RFQItemId != null && rfqItemIds.Contains(q.RFQItemId)
                select q.RFQItemId
            ).Distinct().CountAsync(cancellationToken);
        }

        var orderSnapshot = await BuildOrderOnlySnapshotAsync(orders, scope.MaskAmounts, cancellationToken);
        orderSnapshot.RfqItemCount = rfqItemCount;
        orderSnapshot.RfqCustomerCount = rfqCustomerCount;
        orderSnapshot.RfqToSalesConversionRate = rfqItemCount == 0
            ? null
            : Math.Round((decimal)convertedCount / rfqItemCount * 100m, 2);
        return orderSnapshot;
    }

    public async Task<SalesAnalyticsTodoDto> GetTodoAsync(
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken = default)
    {
        var orders = await BuildScopedOrdersAsync(scope.Summary.UserId, scope, cancellationToken);
        var active = orders.Where(o =>
            o.Status != SellOrderMainStatus.Cancelled && o.Status != SellOrderMainStatus.AuditFailed);

        var receivable = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in active on oi.SellOrderId equals o.Id
            where oi.Status == 0
            select ext.ReceiptAmountNot
        ).SumAsync(cancellationToken);

        var pendingStockOut = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in active on oi.SellOrderId equals o.Id
            where oi.Status == 0 && (ext.StockOutProgressStatus == 0 || ext.StockOutProgressStatus == 1)
            select oi.Id
        ).CountAsync(cancellationToken);

        var pendingInvoice = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in active on oi.SellOrderId equals o.Id
            where oi.Status == 0
            select ext.InvoiceAmountNot
        ).SumAsync(cancellationToken);

        return new SalesAnalyticsTodoDto
        {
            ReceivableAmount = scope.MaskAmounts ? null : receivable,
            PendingStockOutItemCount = pendingStockOut,
            PendingInvoiceAmount = scope.MaskAmounts ? null : pendingInvoice
        };
    }

    private async Task<SalesAnalyticsSnapshotDto> BuildOrderOnlySnapshotAsync(
        IQueryable<SellOrder> ordersInPeriod,
        bool maskAmounts,
        CancellationToken cancellationToken)
    {
        var salesOrderItemCount = await (
            from oi in _db.SellOrderItems.AsNoTracking()
            join o in ordersInPeriod on oi.SellOrderId equals o.Id
            where oi.Status == 0
            select oi.Id
        ).CountAsync(cancellationToken);

        var salesOrderCustomerCount = await ordersInPeriod.Select(o => o.CustomerId).Distinct().CountAsync(cancellationToken);
        var salesAmountApproved = await ordersInPeriod
            .Where(o => o.Status >= SellOrderMainStatus.Approved)
            .SumAsync(o => (decimal?)o.ConvertTotal, cancellationToken) ?? 0m;

        var (salesAmountStockOut, salesAmountReceived) = await SumApprovedLineAmountsAsync(
            ordersInPeriod,
            cancellationToken);

        return new SalesAnalyticsSnapshotDto
        {
            SalesOrderItemCount = salesOrderItemCount,
            SalesOrderCustomerCount = salesOrderCustomerCount,
            SalesAmountApproved = maskAmounts ? null : salesAmountApproved,
            SalesAmountStockOut = maskAmounts ? null : salesAmountStockOut,
            SalesAmountReceived = maskAmounts ? null : salesAmountReceived
        };
    }

    private async Task<(decimal StockOutAmount, decimal ReceivedAmount)> SumApprovedLineAmountsAsync(
        IQueryable<SellOrder> ordersInPeriod,
        CancellationToken cancellationToken)
    {
        var approvedOrders = ordersInPeriod.Where(o => o.Status >= SellOrderMainStatus.Approved);

        var stockOutAmount = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in approvedOrders on oi.SellOrderId equals o.Id
            where oi.Status == 0
            select ext.QtyStockOutActual * oi.ConvertPrice
        ).SumAsync(cancellationToken);

        var receivedAmount = await (
            from ext in _db.SellOrderItemExtends.AsNoTracking()
            join oi in _db.SellOrderItems.AsNoTracking() on ext.Id equals oi.Id
            join o in approvedOrders on oi.SellOrderId equals o.Id
            where oi.Status == 0
            select ext.ReceiptAmountFinish
        ).SumAsync(cancellationToken);

        return (stockOutAmount, receivedAmount);
    }

    private async Task<IQueryable<SellOrder>> BuildScopedOrdersAsync(
        string userId,
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        var q = _db.SellOrders.AsNoTracking();
        q = await _dataPermission.ApplySellOrderDataScopeAsync(userId, q, cancellationToken);
        q = SalesAnalyticsDateFilter.ApplyAnalyticsStatusFilter(q);

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && !BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary)
            && !string.IsNullOrWhiteSpace(scope.SalesUserId)
            && scope.Summary.SaleDataScope != 1)
        {
            q = q.Where(o => o.SalesUserId == scope.SalesUserId);
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
            q = ApplyDepartmentLens(q, scope);

        return q;
    }

    private IQueryable<SellOrder> ApplyDepartmentLens(IQueryable<SellOrder> q, SalesAnalyticsResolvedScope scope)
    {
        var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
        if (string.IsNullOrWhiteSpace(deptId))
            return q;

        if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
        {
            var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                .Where(ud => ud.IsPrimary)
                .Select(ud => ud.UserId);
            return q.Where(o => o.SalesUserId == null || !withPrimary.Contains(o.SalesUserId));
        }

        var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
            .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
            .Select(ud => ud.UserId);
        return q.Where(o => o.SalesUserId != null && userIdsInDept.Contains(o.SalesUserId));
    }

    private async Task<IQueryable<RfqItemJoinRow>> BuildScopedRfqItemJoinAsync(
        string userId,
        SalesAnalyticsResolvedScope scope,
        CancellationToken cancellationToken)
    {
        if (BusinessDepartmentRules.UseSellOrderAssistorOnlyScope(scope.Summary))
        {
            return from item in _db.RFQItems.AsNoTracking()
                   join rfq in _db.RFQs.AsNoTracking() on item.RfqId equals rfq.Id
                   where false
                   select new RfqItemJoinRow { Item = item, Rfq = rfq };
        }

        var baseQ =
            from item in _db.RFQItems.AsNoTracking()
            join rfq in _db.RFQs.AsNoTracking() on item.RfqId equals rfq.Id
            select new { item, rfq };

        var scopedRfqs = await _dataPermission.ApplyRfqMainListDataScopeAsync(
            userId,
            _db.RFQs.AsNoTracking(),
            cancellationToken);
        var rfqIds = scopedRfqs.Select(r => r.Id);
        var q = baseQ.Where(x => rfqIds.Contains(x.rfq.Id));

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Personal
            && !string.IsNullOrWhiteSpace(scope.SalesUserId)
            && scope.Summary.SaleDataScope != 1)
        {
            q = q.Where(x => x.rfq.SalesUserId == scope.SalesUserId);
        }

        if (scope.ViewLevel == SalesAnalyticsViewLevels.Department)
        {
            var deptId = scope.DepartmentId ?? scope.Summary.PrimaryDepartmentId;
            if (!string.IsNullOrWhiteSpace(deptId))
            {
                if (string.Equals(deptId, SalesAnalyticsScopeValidator.UnassignedDepartmentId, StringComparison.OrdinalIgnoreCase))
                {
                    var withPrimary = _db.RbacUserDepartments.AsNoTracking()
                        .Where(ud => ud.IsPrimary)
                        .Select(ud => ud.UserId);
                    q = q.Where(x => x.rfq.SalesUserId == null || !withPrimary.Contains(x.rfq.SalesUserId));
                }
                else
                {
                    var userIdsInDept = _db.RbacUserDepartments.AsNoTracking()
                        .Where(ud => ud.IsPrimary && ud.DepartmentId == deptId)
                        .Select(ud => ud.UserId);
                    q = q.Where(x => x.rfq.SalesUserId != null && userIdsInDept.Contains(x.rfq.SalesUserId));
                }
            }
        }

        return q.Select(x => new RfqItemJoinRow { Item = x.item, Rfq = x.rfq });
    }
}
