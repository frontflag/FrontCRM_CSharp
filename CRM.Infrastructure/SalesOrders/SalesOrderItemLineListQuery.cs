using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.Core.Utilities;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

/// <inheritdoc cref="ISalesOrderItemLineListQuery" />
public sealed class SalesOrderItemLineListQuery : ISalesOrderItemLineListQuery
{
    /// <summary>明细列表单页上限（与翻页查询规范中采购明细一致）。</summary>
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;

    public SalesOrderItemLineListQuery(ApplicationDbContext db, IDataPermissionService dataPermission)
    {
        _db = db;
        _dataPermission = dataPermission;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SellOrderItemLineDto>> GetPagedAsync(
        SellOrderItemLineQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var scopedSo = await _dataPermission.ApplySellOrderDataScopeAsync(
            request.CurrentUserId,
            _db.SellOrders.AsNoTracking(),
            cancellationToken);

        var q =
            from item in _db.SellOrderItems.AsNoTracking()
            join so in scopedSo on item.SellOrderId equals so.Id
            select new { item, so };

        if (request.OrderCreateStart.HasValue)
        {
            var s = SalesAnalyticsDateFilter.ToUtcDateStart(request.OrderCreateStart.Value);
            q = q.Where(x => x.so.CreateTime >= s);
        }

        if (request.OrderCreateEnd.HasValue)
        {
            var e = SalesAnalyticsDateFilter.ToUtcDateEndExclusive(request.OrderCreateEnd.Value);
            q = q.Where(x => x.so.CreateTime < e);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerName))
        {
            var k = request.CustomerName.Trim();
            q = q.Where(x =>
                x.so.CustomerName != null &&
                x.so.CustomerName.ToLower().Contains(k.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserName))
        {
            var sk = request.SalesUserName.Trim();
            q = q.Where(x =>
                x.so.SalesUserName != null &&
                x.so.SalesUserName.ToLower().Contains(sk.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.SellOrderCode))
        {
            var c = request.SellOrderCode.Trim();
            q = q.Where(x =>
                x.so.SellOrderCode != null &&
                x.so.SellOrderCode.ToLower().Contains(c.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.Pn))
        {
            var pn = request.Pn.Trim();
            q = q.Where(x =>
                x.item.PN != null &&
                x.item.PN.ToLower().Contains(pn.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerSo))
        {
            var k = request.CustomerSo.Trim();
            q = q.Where(x =>
                x.item.CustomerSo != null &&
                x.item.CustomerSo.ToLower().Contains(k.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerPn))
        {
            var k = request.CustomerPn.Trim();
            q = q.Where(x =>
                x.item.CustomerPn != null &&
                x.item.CustomerPn.ToLower().Contains(k.ToLower()));
        }

        if (request.PurchaseProgressStatus is >= 0 and <= 2)
        {
            var status = request.PurchaseProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !_db.SellOrderItemExtends.Any(ext => ext.Id == x.item.Id && !ext.IsDeleted)
                    || _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.PurchaseProgressStatus == 0))
                : q.Where(x =>
                    _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.PurchaseProgressStatus == status));
        }

        if (request.StockInProgressStatus is >= 0 and <= 2)
        {
            var status = request.StockInProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !_db.SellOrderItemExtends.Any(ext => ext.Id == x.item.Id && !ext.IsDeleted)
                    || _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.StockInProgressStatus == 0))
                : q.Where(x =>
                    _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.StockInProgressStatus == status));
        }

        if (request.StockOutProgressStatus is >= 0 and <= 2)
        {
            var status = request.StockOutProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !_db.SellOrderItemExtends.Any(ext => ext.Id == x.item.Id && !ext.IsDeleted)
                    || _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.StockOutProgressStatus == 0))
                : q.Where(x =>
                    _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.StockOutProgressStatus == status));
        }

        if (request.ReceiptProgressStatus is >= 0 and <= 2)
        {
            var status = request.ReceiptProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !_db.SellOrderItemExtends.Any(ext => ext.Id == x.item.Id && !ext.IsDeleted)
                    || _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.ReceiptProgressStatus == 0))
                : q.Where(x =>
                    _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.ReceiptProgressStatus == status));
        }

        if (request.InvoiceProgressStatus is >= 0 and <= 2)
        {
            var status = request.InvoiceProgressStatus.Value;
            q = status == 0
                ? q.Where(x =>
                    !_db.SellOrderItemExtends.Any(ext => ext.Id == x.item.Id && !ext.IsDeleted)
                    || _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.InvoiceProgressStatus == 0))
                : q.Where(x =>
                    _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.InvoiceProgressStatus == status));
        }

        if (request.StockOutNotifyProgressStatus is >= 0 and <= 2)
        {
            var notifyStatus = request.StockOutNotifyProgressStatus.Value;
            if (notifyStatus == 0)
            {
                q = q.Where(x =>
                    !_db.SellOrderItemExtends.Any(ext => ext.Id == x.item.Id && !ext.IsDeleted)
                    || _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id && !ext.IsDeleted && ext.QtyStockOutNotify <= 0m));
            }
            else if (notifyStatus == 2)
            {
                q = q.Where(x =>
                    _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id
                        && !ext.IsDeleted
                        && ext.QtyStockOutNotify > 0m
                        && ext.QtyStockOutNotify + 0.0000000001m >= x.item.Qty));
            }
            else
            {
                q = q.Where(x =>
                    _db.SellOrderItemExtends.Any(ext =>
                        ext.Id == x.item.Id
                        && !ext.IsDeleted
                        && ext.QtyStockOutNotify > 0m
                        && ext.QtyStockOutNotify + 0.0000000001m < x.item.Qty));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.TransactionCurrency))
        {
            var kind = request.TransactionCurrency.Trim().ToLowerInvariant();
            if (kind is "rmb" or "cny" or "人民币")
                q = q.Where(x => x.item.Currency == (short)CurrencyCode.RMB);
            else if (kind is "foreign" or "外币")
                q = q.Where(x => x.item.Currency != (short)CurrencyCode.RMB);
        }

        if (!string.IsNullOrWhiteSpace(request.SalesUserId))
        {
            var uid = request.SalesUserId.Trim();
            q = q.Where(x => x.so.SalesUserId == uid);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            var cid = request.CustomerId.Trim();
            q = q.Where(x => x.so.CustomerId == cid);
        }

        if (request.StockOutPending)
        {
            q = q.Where(x =>
                x.item.Status == 0
                && x.so.Status != SellOrderMainStatus.Cancelled
                && x.so.Status != SellOrderMainStatus.AuditFailed
                && _db.SellOrderItemExtends.Any(ext =>
                    ext.Id == x.item.Id
                    && !ext.IsDeleted
                    && (ext.StockOutProgressStatus == 0 || ext.StockOutProgressStatus == 1)));
        }

        if (request.InvoicePending)
        {
            q = q.Where(x =>
                x.item.Status == 0
                && x.so.Status != SellOrderMainStatus.Cancelled
                && x.so.Status != SellOrderMainStatus.AuditFailed
                && _db.SellOrderItemExtends.Any(ext =>
                    ext.Id == x.item.Id
                    && !ext.IsDeleted
                    && (ext.InvoiceProgressStatus < 2 || ext.InvoiceAmountNot > 0)));
        }

        var total = await q.CountAsync(cancellationToken);

        var ordered = q
            .OrderByDescending(x => x.so.CreateTime)
            .ThenBy(x => x.item.SellOrderItemCode);

        var raw = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                SellOrderItemId = x.item.Id,
                SellOrderId = x.item.SellOrderId,
                SellOrderCode = x.so.SellOrderCode,
                SellOrderItemCode = x.item.SellOrderItemCode,
                OrderStatus = (short)x.so.Status,
                OrderCreateTime = x.so.CreateTime,
                CustomerId = x.so.CustomerId,
                CustomerName = x.so.CustomerName,
                SalesUserName = x.so.SalesUserName,
                PN = x.item.PN,
                Brand = x.item.Brand,
                CustomerSo = x.item.CustomerSo,
                CustomerPn = x.item.CustomerPn,
                Qty = x.item.Qty,
                Price = x.item.Price,
                Currency = x.item.Currency,
                ConvertPrice = x.item.ConvertPrice,
                ItemStatus = x.item.Status
            })
            .ToListAsync(cancellationToken);

        var slice = raw
            .Select(r => MapLine(
                r.SellOrderItemId,
                r.SellOrderId,
                r.SellOrderCode,
                r.SellOrderItemCode,
                r.OrderStatus,
                r.OrderCreateTime,
                r.CustomerId,
                r.CustomerName,
                r.SalesUserName,
                r.PN,
                r.Brand,
                r.CustomerSo,
                r.CustomerPn,
                r.Qty,
                r.Price,
                r.Currency,
                r.ConvertPrice,
                r.ItemStatus))
            .ToList();

        return new PagedResult<SellOrderItemLineDto>
        {
            Items = slice,
            TotalCount = total,
            PageIndex = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<List<SellOrderItemLineDto>> GetByIdsAsync(
        IReadOnlyList<string> sellOrderItemIds,
        CancellationToken cancellationToken = default)
    {
        if (sellOrderItemIds == null || sellOrderItemIds.Count == 0)
            return new List<SellOrderItemLineDto>();

        var ids = sellOrderItemIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();
        if (ids.Count == 0)
            return new List<SellOrderItemLineDto>();

        var raw = await (
            from item in _db.SellOrderItems.AsNoTracking()
            join so in _db.SellOrders.AsNoTracking() on item.SellOrderId equals so.Id
            where ids.Contains(item.Id)
            orderby item.SellOrderItemCode
            select new
            {
                SellOrderItemId = item.Id,
                SellOrderId = item.SellOrderId,
                SellOrderCode = so.SellOrderCode,
                SellOrderItemCode = item.SellOrderItemCode,
                OrderStatus = (short)so.Status,
                OrderCreateTime = so.CreateTime,
                CustomerId = so.CustomerId,
                CustomerName = so.CustomerName,
                SalesUserName = so.SalesUserName,
                PN = item.PN,
                Brand = item.Brand,
                CustomerSo = item.CustomerSo,
                CustomerPn = item.CustomerPn,
                Qty = item.Qty,
                Price = item.Price,
                Currency = item.Currency,
                ConvertPrice = item.ConvertPrice,
                ItemStatus = item.Status
            }).ToListAsync(cancellationToken);

        return raw
            .Select(r => MapLine(
                r.SellOrderItemId,
                r.SellOrderId,
                r.SellOrderCode,
                r.SellOrderItemCode,
                r.OrderStatus,
                r.OrderCreateTime,
                r.CustomerId,
                r.CustomerName,
                r.SalesUserName,
                r.PN,
                r.Brand,
                r.CustomerSo,
                r.CustomerPn,
                r.Qty,
                r.Price,
                r.Currency,
                r.ConvertPrice,
                r.ItemStatus))
            .ToList();
    }

    private static SellOrderItemLineDto MapLine(
        string sellOrderItemId,
        string sellOrderId,
        string sellOrderCode,
        string? sellOrderItemCode,
        short orderStatus,
        DateTime orderCreateTime,
        string? customerId,
        string? customerName,
        string? salesUserName,
        string? pn,
        string? brand,
        string? customerSo,
        string? customerPn,
        decimal qty,
        decimal price,
        short currency,
        decimal convertPrice,
        short itemStatus)
    {
        var lineTotal = Math.Round(qty * price, 2, MidpointRounding.AwayFromZero);
        decimal? usdUnit;
        decimal? usdLine;
        if (currency == (short)CurrencyCode.USD)
        {
            usdUnit = convertPrice;
            usdLine = Math.Round(qty * convertPrice, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            usdUnit = convertPrice != 0m ? convertPrice : null;
            usdLine = usdUnit.HasValue
                ? Math.Round(qty * usdUnit.Value, 2, MidpointRounding.AwayFromZero)
                : null;
        }

        return new SellOrderItemLineDto
        {
            SellOrderItemId = sellOrderItemId,
            SellOrderId = sellOrderId,
            SellOrderCode = sellOrderCode ?? string.Empty,
            SellOrderItemCode = sellOrderItemCode,
            OrderStatus = orderStatus,
            OrderCreateTime = orderCreateTime,
            CustomerId = customerId,
            CustomerName = customerName,
            SalesUserName = salesUserName,
            PN = pn,
            Brand = brand,
            CustomerSo = customerSo,
            CustomerPn = customerPn,
            Qty = qty,
            Price = price,
            LineTotal = lineTotal,
            Currency = currency,
            UsdUnitPrice = usdUnit,
            UsdLineTotal = usdLine,
            ItemStatus = itemStatus
        };
    }
}
