using CRM.Core.Constants;
using CRM.Core.Interfaces;
using CRM.Core.Models.Sales;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.SalesOrders;

/// <inheritdoc cref="ISalesOrderItemLineListQuery" />
public sealed partial class SalesOrderItemLineListQuery : ISalesOrderItemLineListQuery
{
    /// <summary>明细列表单页上限（与翻页查询规范中采购明细一致）。</summary>
    public const int MaxPageSize = 100;

    private readonly ApplicationDbContext _db;
    private readonly IDataPermissionService _dataPermission;
    private readonly IFinanceExchangeRateService _exchangeRateService;

    public SalesOrderItemLineListQuery(
        ApplicationDbContext db,
        IDataPermissionService dataPermission,
        IFinanceExchangeRateService exchangeRateService)
    {
        _db = db;
        _dataPermission = dataPermission;
        _exchangeRateService = exchangeRateService;
    }

    /// <inheritdoc />
    public async Task<PagedResult<SellOrderItemLineDto>> GetPagedAsync(
        SellOrderItemLineQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var filtered = await SalesOrderItemLineListFilter.BuildFilteredJoinQueryAsync(
            _db, _dataPermission, request, cancellationToken);
        var total = await filtered.CountAsync(cancellationToken);

        var raw = await filtered
            .OrderByDescending(x => x.So.CreateTime)
            .ThenBy(x => x.Item.SellOrderItemCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                SellOrderItemId = x.Item.Id,
                SellOrderId = x.Item.SellOrderId,
                SellOrderCode = x.So.SellOrderCode,
                SellOrderItemCode = x.Item.SellOrderItemCode,
                OrderStatus = (short)x.So.Status,
                OrderCreateTime = x.So.CreateTime,
                CustomerId = x.So.CustomerId,
                CustomerName = x.So.CustomerName,
                CustomerCode = x.So.CustomerCode,
                SalesUserName = x.So.SalesUserName,
                PN = x.Item.PN,
                Brand = x.Item.Brand,
                CustomerSo = x.Item.CustomerSo,
                CustomerPn = x.Item.CustomerPn,
                Qty = x.Item.Qty,
                Price = x.Item.Price,
                Currency = x.Item.Currency,
                ConvertPrice = x.Item.ConvertPrice,
                ItemStatus = x.Item.Status
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
                r.CustomerCode,
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
                CustomerCode = so.CustomerCode,
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
                r.CustomerCode,
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
        string? customerCode,
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
            CustomerCode = customerCode,
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
