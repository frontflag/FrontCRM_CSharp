using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
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
            var s = request.OrderCreateStart.Value;
            q = q.Where(x => x.so.CreateTime >= s);
        }

        if (request.OrderCreateEnd.HasValue)
        {
            var e = request.OrderCreateEnd.Value.Date.AddDays(1);
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
