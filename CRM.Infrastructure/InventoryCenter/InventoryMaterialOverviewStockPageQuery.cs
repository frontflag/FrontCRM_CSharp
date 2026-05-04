using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Interfaces;
using CRM.Core.Models.Inventory;
using CRM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.InventoryCenter;

/// <summary>
/// 总览分页在 <c>stock</c> 上完成；排序为 <c>ModifyTime ?? CreateTime</c> 降序（与「按分桶行更新时间」近似，与全量内存排序的 <c>LastMoveTime</c> 可能略有差异，见文档）。
/// </summary>
public sealed class InventoryMaterialOverviewStockPageQuery : IInventoryMaterialOverviewStockPageQuery
{
    private readonly ApplicationDbContext _db;

    public InventoryMaterialOverviewStockPageQuery(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<StockInfo> Items, int TotalCount)> GetStockPageAsync(
        string? warehouseId,
        string? materialModel,
        string? stockCode,
        short? stockType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var p = page < 1 ? 1 : page;
        var ps = pageSize < 1 ? 20 : Math.Min(pageSize, IInventoryMaterialOverviewStockPageQuery.MaxPageSize);

        var wh = warehouseId?.Trim();
        var modelK = materialModel?.Trim().ToLowerInvariant();
        var codeK = stockCode?.Trim().ToLowerInvariant();

        var q =
            from s in _db.Stocks.AsNoTracking()
            join m in _db.Materials.AsNoTracking() on s.MaterialId equals m.Id into mj
            from m in mj.DefaultIfEmpty()
            select new { s, m };

        if (!string.IsNullOrEmpty(wh))
            q = q.Where(x => x.s.WarehouseId == wh);

        if (stockType is >= 1 and <= 3)
            q = q.Where(x => x.s.StockType == stockType.Value);

        if (!string.IsNullOrEmpty(codeK))
            q = q.Where(x => x.s.StockCode != null && x.s.StockCode.ToLower().Contains(codeK));

        if (!string.IsNullOrEmpty(modelK))
        {
            q = q.Where(x =>
                (x.s.PurchasePn != null && x.s.PurchasePn.ToLower().Contains(modelK))
                || (x.m != null && x.m.MaterialModel != null && x.m.MaterialModel.ToLower().Contains(modelK))
                || (x.m != null && x.m.MaterialName.ToLower().Contains(modelK)));
        }

        var ordered = q
            .OrderByDescending(x => x.s.ModifyTime ?? x.s.CreateTime)
            .ThenBy(x => x.s.WarehouseId)
            .ThenBy(x => x.s.StockType)
            .ThenBy(x => x.s.MaterialId)
            .ThenBy(x => x.s.Id);

        var total = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => x.s)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
