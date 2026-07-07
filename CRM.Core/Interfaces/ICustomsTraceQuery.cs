using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>报关业务溯源：沿到货/出库通知、报关明细解析报关单号与原供应商（展示用）。</summary>
public interface ICustomsTraceQuery
{
    /// <summary>批量解析到货通知关联的报关单（key = notify.Id）。</summary>
    Task<IReadOnlyDictionary<string, CustomsTraceLinkDto>> GetByStockInNotifyIdsAsync(
        IEnumerable<string> notifyIds,
        CancellationToken cancellationToken = default);

    /// <summary>批量解析出库通知关联的报关单（key = stockout_notify.Id）。</summary>
    Task<IReadOnlyDictionary<string, CustomsTraceLinkDto>> GetByStockOutNotifyIdsAsync(
        IEnumerable<string> notifyIds,
        CancellationToken cancellationToken = default);

    /// <summary>报关到货通知：回填报关单号、原供应商名称等展示字段（不写库）。</summary>
    Task EnrichCustomsStockInNotifiesAsync(
        IReadOnlyList<StockInNotify> rows,
        CancellationToken cancellationToken = default);

    /// <summary>出库通知列表：回填报关单号（不写库）。</summary>
    Task EnrichStockOutRequestListItemsAsync(
        IReadOnlyList<StockOutRequestListItemDto> rows,
        CancellationToken cancellationToken = default);

    /// <summary>出库单列表：回填报关单号（不写库）。</summary>
    Task EnrichStockOutListItemsAsync(
        IReadOnlyList<StockOutListItemDto> rows,
        CancellationToken cancellationToken = default);

    /// <summary>报关出库通知详情：轻量报关摘要（报关单 + 报关公司）。</summary>
    Task<StockOutCustomsSummaryDto?> ResolveStockOutNotifyCustomsSummaryAsync(
        string notifyId,
        short stockOutType,
        CancellationToken cancellationToken = default);

    /// <summary>报关出库单详情：轻量报关摘要。</summary>
    Task<StockOutCustomsSummaryDto?> ResolveStockOutCustomsSummaryAsync(
        StockOut stockOut,
        CancellationToken cancellationToken = default);

    /// <summary>按报关单 ID 解析轻量摘要（装箱单等直接持有 declaration_id 的场景）。</summary>
    Task<StockOutCustomsSummaryDto?> ResolveCustomsSummaryByDeclarationIdAsync(
        string? declarationId,
        CancellationToken cancellationToken = default);
}

public sealed class CustomsTraceLinkDto
{
    public string CustomsDeclarationId { get; set; } = string.Empty;
    public string CustomsDeclarationCode { get; set; } = string.Empty;
    public string? VendorId { get; set; }
    public string? VendorName { get; set; }
    public string? CustomsBrokerId { get; set; }
    public string? CustomsBrokerName { get; set; }
    public short? CustomsClearanceStatus { get; set; }
}

/// <summary>报关出库侧详情页轻量摘要（Hub 跳转 + 报关公司名）。</summary>
public sealed class StockOutCustomsSummaryDto
{
    public string DeclarationId { get; set; } = string.Empty;
    public string DeclarationCode { get; set; } = string.Empty;
    public string? CustomsBrokerId { get; set; }
    public string? CustomsBrokerName { get; set; }
    public short? CustomsClearanceStatus { get; set; }
}
