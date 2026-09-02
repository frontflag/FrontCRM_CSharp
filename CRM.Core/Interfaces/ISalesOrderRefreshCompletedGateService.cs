using CRM.Core.Models.Sales;

namespace CRM.Core.Interfaces;

/// <summary>分面刷新已完结下游：预检与确认门控（不含刷新状态、刷新客户）。</summary>
public interface ISalesOrderRefreshCompletedGateService
{
    Task<SalesOrderRefreshCompletedPreview> PreviewAsync(
        string salesOrderId,
        SalesOrderRefreshFacet facet,
        CancellationToken cancellationToken = default);

    /// <summary>参数不允许则阻断；有已完结且未确认则拒绝。</summary>
    Task EnsureAllowedAsync(
        string salesOrderId,
        SalesOrderRefreshFacet facet,
        bool confirmCompleted,
        CancellationToken cancellationToken = default);
}

public class SalesOrderRefreshCompletedPreview
{
    public string Facet { get; set; } = "status";
    public bool CanProceed { get; set; }
    public string? BlockReason { get; set; }
    /// <summary>销售参数是否允许本分面覆盖已完结下游。</summary>
    public bool AllowCompletedParam { get; set; }
    /// <summary>本次将改写的已完结下游说明（参数允许时供确认；不允许时亦用于阻断文案）。</summary>
    public List<string> CompletedDocuments { get; set; } = new();
    public bool HasCompleted => CompletedDocuments.Count > 0;
}
