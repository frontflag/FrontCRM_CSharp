using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <inheritdoc cref="IPurchaseOrderItemListQuery" />
public sealed class PurchaseOrderItemListQuery : IPurchaseOrderItemListQuery
{
    /// <summary>明细列表单页上限（与产品确认）。</summary>
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public PurchaseOrderItemListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseOrderItemListLineRaw>> GetPagedAsync(
        PurchaseOrderItemListQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var scopedPo = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(
            request.CurrentUserId,
            _db.PurchaseOrders.AsNoTracking(),
            cancellationToken);

        var q =
            from item in _db.PurchaseOrderItems.AsNoTracking()
            join po in scopedPo on item.PurchaseOrderId equals po.Id
            join ext in _db.PurchaseOrderItemExtends.AsNoTracking() on item.Id equals ext.Id into extGroup
            from ext in extGroup.DefaultIfEmpty()
            select new { item, po, ext };

        if (request.StartDate.HasValue)
            q = q.Where(x => x.po.CreateTime >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            q = q.Where(x => x.po.CreateTime <= request.EndDate.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(request.PurchaseOrderCode))
        {
            var c = request.PurchaseOrderCode.Trim();
            q = q.Where(x =>
                x.po.PurchaseOrderCode != null &&
                x.po.PurchaseOrderCode.ToLower().Contains(c.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.VendorName))
        {
            var v = request.VendorName.Trim();
            q = q.Where(x =>
                x.po.VendorName != null &&
                x.po.VendorName.ToLower().Contains(v.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.PurchaseUserName))
        {
            var p = request.PurchaseUserName.Trim();
            q = q.Where(x =>
                x.po.PurchaseUserName != null &&
                x.po.PurchaseUserName.ToLower().Contains(p.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Pn))
        {
            var pn = request.Pn.Trim();
            q = q.Where(x =>
                x.item.PN != null &&
                x.item.PN.ToLower().Contains(pn.ToLower()));
        }

        if (request.OrderType.HasValue)
            q = q.Where(x => x.po.Type == request.OrderType.Value);

        var total = await q.CountAsync(cancellationToken);

        var ordered = q
            .OrderByDescending(x => x.po.CreateTime)
            .ThenBy(x => x.item.PurchaseOrderItemCode);

        var slice = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PurchaseOrderItemListLineRaw
            {
                PurchaseOrderItemId = x.item.Id,
                PurchaseOrderId = x.item.PurchaseOrderId,
                PurchaseOrderItemCode = x.item.PurchaseOrderItemCode,
                PurchaseOrderCode = x.po.PurchaseOrderCode,
                PurchaseOrderType = x.po.Type >= 1 && x.po.Type <= 3 ? x.po.Type : (short)1,
                OrderStatus = x.po.Status,
                OrderCreateTime = x.po.CreateTime,
                PurchaseUserName = x.po.PurchaseUserName,
                CreateByUserId = x.po.CreateByUserId,
                VendorId = x.item.VendorId,
                VendorName = x.po.VendorName,
                Pn = x.item.PN,
                Brand = x.item.Brand,
                ItemStatus = x.item.Status,
                FinancePaymentStatus = x.item.FinancePaymentStatus,
                PurchaseProgressStatus = x.ext != null ? x.ext.PurchaseProgressStatus : (short)0,
                StockInProgressStatus = x.ext != null ? x.ext.StockInProgressStatus : (short)0,
                PaymentProgressStatus = x.ext != null ? x.ext.PaymentProgressStatus : (short)0,
                InvoiceProgressStatus = x.ext != null ? x.ext.InvoiceProgressStatus : (short)0,
                PaymentAmountRequested = x.ext != null ? x.ext.PaymentAmountRequested : 0m,
                Qty = x.item.Qty,
                Cost = x.item.Cost,
                Currency = x.item.Currency,
                DeliveryDate = x.item.DeliveryDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrderItemListLineRaw>
        {
            Items = slice,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }
}
