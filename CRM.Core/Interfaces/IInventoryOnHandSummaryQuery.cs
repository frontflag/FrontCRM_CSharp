using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Interfaces;

/// <summary>库存中心在库汇总：<c>stock_item</c> 在库数量 &gt; 0，按型号+品牌及可选维度分组。</summary>
public interface IInventoryOnHandSummaryQuery
{
    public const int MaxPageSize = 2000;

    Task<InventoryOnHandSummaryPagedResult> GetPagedAsync(
        InventoryOnHandSummaryQueryRequest request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryOnHandSummaryQueryRequest
{
    public string? MaterialModel { get; set; }
    public string? PurchaseBrand { get; set; }
    public short? StockType { get; set; }
    public string? WarehouseId { get; set; }
    public bool GroupByStockType { get; set; }
    public bool GroupByWarehouse { get; set; }
    public string? CurrentUserId { get; set; }
}

public sealed class InventoryOnHandAmountDto
{
    public short Currency { get; set; }
    public decimal Amount { get; set; }
}

public sealed class InventoryOnHandSummaryRowDto
{
    public string? MaterialModel { get; set; }
    public string? PurchaseBrand { get; set; }
    public short? StockType { get; set; }
    public string? WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
    public int OnHandQty { get; set; }
    public List<InventoryOnHandAmountDto> Amounts { get; set; } = new();
}

public sealed class InventoryOnHandSummaryPagedResult
{
    public IReadOnlyList<InventoryOnHandSummaryRowDto> Items { get; set; } =
        Array.Empty<InventoryOnHandSummaryRowDto>();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<short> Currencies { get; set; } = Array.Empty<short>();
    /// <summary>当前筛选全量在库数量（PCS，与分页无关）。</summary>
    public int TotalOnHandQty { get; set; }
    /// <summary>当前筛选全量原币库存金额（顺序与 <see cref="Currencies"/> 一致）。</summary>
    public List<InventoryOnHandAmountDto> TotalAmounts { get; set; } = new();
}
