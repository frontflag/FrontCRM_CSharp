using CRM.Core.Interfaces;
using CRM.Core.Models.Finance;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Finance;

/// <summary>付款单主表列表：EF 侧 <c>CountAsync</c> + <c>Skip</c>/<c>Take</c>。</summary>
public sealed class FinancePaymentListQuery : IFinancePaymentListQuery
{
    public const int MaxPageSize = 2000;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermissionService;

    public FinancePaymentListQuery(ApplicationDbContext db, IDataPermissionService dataPermissionService)
    {
        _db = db;
        _dataPermissionService = dataPermissionService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<FinancePayment>> GetPagedAsync(
        FinancePaymentQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var q = _db.FinancePayments.AsNoTracking();
        q = await _dataPermissionService.ApplyFinancePaymentListDataScopeAsync(
            request.CurrentUserId,
            q,
            _db.Vendors.AsNoTracking(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.FinancePaymentCode))
        {
            var c = request.FinancePaymentCode.Trim().ToLowerInvariant();
            q = q.Where(p => p.FinancePaymentCode.ToLower().Contains(c));
        }
        else if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var k = request.Keyword.Trim().ToLowerInvariant();
            q = q.Where(p =>
                (p.FinancePaymentCode != null && p.FinancePaymentCode.ToLower().Contains(k)) ||
                (p.VendorName != null && p.VendorName.ToLower().Contains(k)));
        }

        if (!string.IsNullOrWhiteSpace(request.FreightForwarderOrderNo))
            q = await ApplyFreightForwarderOrderNoFilterAsync(q, request.FreightForwarderOrderNo.Trim(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.BankSlipNo))
        {
            var b = request.BankSlipNo.Trim().ToLowerInvariant();
            q = q.Where(p => p.BankSlipNo != null && p.BankSlipNo.ToLower().Contains(b));
        }

        if (request.PaymentMode.HasValue)
            q = q.Where(p => p.PaymentMode == request.PaymentMode.Value);

        if (!string.IsNullOrWhiteSpace(request.VendorName))
        {
            var v = request.VendorName.Trim().ToLowerInvariant();
            q = q.Where(p => p.VendorName != null && p.VendorName.ToLower().Contains(v));
        }

        if (!string.IsNullOrWhiteSpace(request.Remark))
        {
            var r = request.Remark.Trim().ToLowerInvariant();
            q = q.Where(p => p.Remark != null && p.Remark.ToLower().Contains(r));
        }

        if (request.Status.HasValue)
            q = q.Where(p => p.Status == request.Status.Value);

        if (request.StartDate.HasValue)
            q = q.Where(p => p.PaymentDate != null && p.PaymentDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(p => p.PaymentDate != null && p.PaymentDate <= request.EndDate.Value.AddDays(1));

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(p => p.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancePayment>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    private async Task<IQueryable<FinancePayment>> ApplyFreightForwarderOrderNoFilterAsync(
        IQueryable<FinancePayment> q,
        string freightForwarderOrderNo,
        CancellationToken cancellationToken)
    {
        var ff = freightForwarderOrderNo.ToLowerInvariant();
        var matchingPoIds = await _db.PurchaseOrders.AsNoTracking()
            .Where(po =>
                po.FreightForwarderOrderNo != null &&
                po.FreightForwarderOrderNo.ToLower().Contains(ff))
            .Select(po => po.Id)
            .ToListAsync(cancellationToken);

        if (matchingPoIds.Count == 0)
            return q.Where(_ => false);

        var matchingPoiIds = await _db.PurchaseOrderItems.AsNoTracking()
            .Where(poi => matchingPoIds.Contains(poi.PurchaseOrderId))
            .Select(poi => poi.Id)
            .ToListAsync(cancellationToken);

        return q.Where(p => _db.FinancePaymentItems.Any(item =>
            item.FinancePaymentId == p.Id &&
            ((item.PurchaseOrderId != null && matchingPoIds.Contains(item.PurchaseOrderId))
             || (item.PurchaseOrderItemId != null && matchingPoiIds.Contains(item.PurchaseOrderItemId)))));
    }
}
