using CRM.Core.Interfaces;
using CRM.Core.Models.Purchase;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.PurchaseOrders;

/// <inheritdoc cref="IPurchaseOrderItemListQuery" />
public sealed partial class PurchaseOrderItemListQuery : IPurchaseOrderItemListQuery
{
    /// <summary>明细列表单页上限（与产品确认）。</summary>
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IFinanceExchangeRateService _exchangeRateService;

    public PurchaseOrderItemListQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IFinanceExchangeRateService exchangeRateService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _exchangeRateService = exchangeRateService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<PurchaseOrderItemListLineRaw>> GetPagedAsync(
        PurchaseOrderItemListQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = await PurchaseOrderItemListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, request, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);

        var slice = await filtered
            .OrderByDescending(x => x.Po.CreateTime)
            .ThenBy(x => x.Item.PurchaseOrderItemCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapLineRaw(x))
            .ToListAsync(cancellationToken);

        if (request.StockingPurchaseSharedList)
            await StockingAvailableQtyLookup.ApplyAsync(_db, slice, cancellationToken);

        return new PagedResult<PurchaseOrderItemListLineRaw>
        {
            Items = slice,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<List<PurchaseOrderItemListLineRaw>> GetByIdsAsync(
        IReadOnlyList<string> purchaseOrderItemIds,
        string? currentUserId = null,
        bool applyDataScope = true,
        CancellationToken cancellationToken = default)
    {
        if (purchaseOrderItemIds == null || purchaseOrderItemIds.Count == 0)
            return new List<PurchaseOrderItemListLineRaw>();

        var idList = purchaseOrderItemIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (idList.Count == 0)
            return new List<PurchaseOrderItemListLineRaw>();

        var poQuery = _db.PurchaseOrders.AsNoTracking();
        if (applyDataScope)
        {
            poQuery = await _dataPermission.ApplyPurchaseOrderDataScopeAsync(
                currentUserId,
                poQuery,
                cancellationToken);
        }

        return await (
            from item in _db.PurchaseOrderItems.AsNoTracking()
            where idList.Contains(item.Id)
            join po in poQuery on item.PurchaseOrderId equals po.Id
            join ext in _db.PurchaseOrderItemExtends.AsNoTracking().Where(e => !e.IsDeleted)
                on item.Id equals ext.Id into extGroup
            from ext in extGroup.DefaultIfEmpty()
            select MapLineRaw(new PurchaseOrderItemLineJoin { Item = item, Po = po, Ext = ext })
        ).ToListAsync(cancellationToken);
    }

    private static PurchaseOrderItemListLineRaw MapLineRaw(PurchaseOrderItemLineJoin x) =>
        new()
        {
            PurchaseOrderItemId = x.Item.Id,
            PurchaseOrderId = x.Item.PurchaseOrderId,
            PurchaseOrderItemCode = x.Item.PurchaseOrderItemCode,
            PurchaseOrderCode = x.Po.PurchaseOrderCode,
            FreightForwarderOrderNo = x.Po.FreightForwarderOrderNo,
            PurchaseOrderType = x.Po.Type >= 1 && x.Po.Type <= 3 ? x.Po.Type : (short)1,
            OrderStatus = x.Po.Status,
            OrderCreateTime = x.Po.CreateTime,
            PurchaseUserName = x.Po.PurchaseUserName,
            CreateByUserId = x.Po.CreateByUserId,
            VendorId = x.Item.VendorId,
            VendorName = x.Po.VendorName,
            VendorCode = x.Po.VendorCode,
            Pn = x.Item.PN,
            Brand = x.Item.Brand,
            ItemStatus = x.Item.Status,
            FinancePaymentStatus = x.Item.FinancePaymentStatus,
            PurchaseProgressStatus = x.Ext != null ? x.Ext.PurchaseProgressStatus : (short)0,
            StockInProgressStatus = x.Ext != null ? x.Ext.StockInProgressStatus : (short)0,
            PaymentProgressStatus = x.Ext != null ? x.Ext.PaymentProgressStatus : (short)0,
            InvoiceProgressStatus = x.Ext != null ? x.Ext.InvoiceProgressStatus : (short)0,
            PaymentAmount = x.Ext != null
                ? x.Ext.PaymentAmount
                : Math.Round(x.Item.Qty * x.Item.Cost, 2, MidpointRounding.AwayFromZero),
            PaymentAmountRequested = x.Ext != null ? x.Ext.PaymentAmountRequested : 0m,
            QtyStockInNotifyExpectSum = x.Ext != null ? x.Ext.QtyStockInNotifyExpectSum : 0m,
            QtyStockInNotifyNot = x.Ext != null ? x.Ext.QtyStockInNotifyNot : x.Item.Qty,
            Qty = x.Item.Qty,
            Cost = x.Item.Cost,
            Currency = x.Item.Currency,
            DeliveryDate = x.Item.DeliveryDate
        };
}
