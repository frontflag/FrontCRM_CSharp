namespace CRM.Core.Interfaces;

public interface ICustomsPendlistService
{
    Task<IReadOnlyList<CustomsPendlistListItemDto>> GetListAsync(
        short? status,
        string? keyword,
        int take,
        string? currentUserId = null,
        CancellationToken cancellationToken = default);

    Task<CreateCustomsOutNotifyResultDto> CreateCustomsOutNotifyAsync(
        string pendlistId,
        string? actingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>销售出库通知删除前校验：已生成报关出库通知则禁止。</summary>
    Task EnsureSalesNotifyDeletableAsync(string salesStockOutNotifyId);

    /// <summary>销售出库通知软删后：同步取消 pendlist。</summary>
    Task CancelBySalesStockOutNotifyAsync(string salesStockOutNotifyId, string? actingUserId);

    /// <summary>报关出库通知删除后：回退 pendlist 至待处理并解除关联。</summary>
    Task RevertPendlistOnCustomsOutNotifyDeleteAsync(string customsStockOutNotifyId, string? actingUserId);

    /// <summary>
    /// 强制删除待报关记录。确认码为 pendlist Guid。
    /// 存在报关出库通知 / 装箱 / 报关明细等下游时拒绝。
    /// </summary>
    Task ForceDeleteAsync(
        string id,
        string confirmPendlistId,
        string actingUserId,
        string? actingUserName);
}

public sealed class CustomsPendlistListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string SalesStockOutNotifyId { get; set; } = string.Empty;
    public string? SalesStockOutNotifyCode { get; set; }
    public string SellOrderItemId { get; set; } = string.Empty;
    public string? SellOrderItemCode { get; set; }
    public int Qty { get; set; }
    public short Status { get; set; }
    public string? CustomsStockOutNotifyId { get; set; }
    public string? CustomsStockOutNotifyCode { get; set; }
    public string? OverseasWarehouseId { get; set; }
    public string? OverseasWarehouseName { get; set; }
    public string? SalesOrderId { get; set; }
    public string? SalesOrderCode { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateByUserId { get; set; }
    public string? CreateUserDisplay { get; set; }
}

public sealed class CreateCustomsOutNotifyResultDto
{
    public string PendlistId { get; set; } = string.Empty;
    public string CustomsStockOutNotifyId { get; set; } = string.Empty;
    public string CustomsStockOutNotifyCode { get; set; } = string.Empty;
    public short PendlistStatus { get; set; }
}
