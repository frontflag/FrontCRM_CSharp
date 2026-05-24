using CRM.Core.Constants;
using CRM.Core.Models;

namespace CRM.Core.Interfaces;

public interface IPackingService
{
    Task<PagedResult<PackingListItemDto>> GetPackingListPagedAsync(
        PackingListQueryRequest? filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PackingItemListRowDto>> GetPackingItemListPagedAsync(
        string? keyword,
        string? packingCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PackingDetailDto?> GetPackingByIdAsync(
        string packingId,
        CancellationToken cancellationToken = default);

    /// <summary>按出库通知 Id 解析关联装箱单并返回详情（含全部装箱明细行）。</summary>
    Task<PackingDetailDto?> GetPackingByStockOutRequestIdAsync(
        string stockOutRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>新建装箱单前预览（校验出库通知并返回拟装箱明细）。</summary>
    Task<PackingDraftFromStockOutRequestsDto> GetDraftFromStockOutRequestsAsync(
        IReadOnlyList<string> stockOutRequestIds,
        CancellationToken cancellationToken = default);

    /// <summary>从多条出库通知生成一张装箱单（同客户、销行币别一致）。</summary>
    Task<PackingCreateResultDto> CreateFromStockOutRequestsAsync(
        IReadOnlyList<string> stockOutRequestIds,
        PackingCreateExtras? extras,
        string? actingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>按装箱明细关联的销售行，解析用于 Invoice/Packing 打印的出库单 Id（最新一条）。</summary>
    Task<string?> ResolveLinkedStockOutIdForPrintAsync(
        string packingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按所选装箱单解析出库通知 Id。
    /// <paramref name="forPicking"/> 为 true 时：仅要求装箱单已确认，按明细关联通知解析（不校验通知为已装箱）；
    /// 为 false 时：用于批量出库解析（不校验出库通知状态）。
    /// </summary>
    Task<PackingStockOutRequestsResolveDto> ResolveStockOutRequestIdsFromPackingsAsync(
        IReadOnlyList<string> packingIds,
        bool forPicking = false,
        CancellationToken cancellationToken = default);

    /// <summary>批量出库：校验装箱单后按拣货结果自动生成出库单（不经过执行出库页）。</summary>
    Task<PackingBatchStockOutResultDto> BatchExecuteStockOutFromPackingsAsync(
        IReadOnlyList<string> packingIds,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>确认装箱单：仅 <see cref="PackingStatusCode.New"/> 可转为 <see cref="PackingStatusCode.Confirmed"/>。</summary>
    Task ConfirmPackingAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 备货完成：仅 <see cref="PackingStatusCode.Picked"/> 可转为 <see cref="PackingStatusCode.Ready"/>；
    /// 同时将本装箱单关联的未取消拣货任务 <c>pickingtask.Status</c> 置为 100。
    /// </summary>
    Task MarkPackingReadyAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>删除装箱单：仅 <see cref="PackingStatusCode.New"/>；软删主表与明细，回滚关联出库通知为待装箱。</summary>
    Task DeletePackingAsync(
        string packingId,
        string? actingUserId = null,
        CancellationToken cancellationToken = default);
}

public class PackingStockOutRequestsResolveDto
{
    public List<string> StockOutRequestIds { get; set; } = new();
    /// <summary>出库通知与装箱单对应关系（执行出库时写入 <c>stock_out_item.packing_id</c>）。</summary>
    public List<PackingStockOutRequestLinkDto> Links { get; set; } = new();
    public string? CustomerId { get; set; }
    public int PackingCount { get; set; }
}

public class PackingStockOutRequestLinkDto
{
    public string StockOutRequestId { get; set; } = string.Empty;
    public string PackingId { get; set; } = string.Empty;
}

public class PackingBatchStockOutResultDto
{
    public IReadOnlyList<PackingBatchStockOutLineDto> Lines { get; set; } = Array.Empty<PackingBatchStockOutLineDto>();
}

public class PackingBatchStockOutLineDto
{
    public string PackingId { get; set; } = string.Empty;
    public string? PackingCode { get; set; }
    public string StockOutId { get; set; } = string.Empty;
    public string StockOutCode { get; set; } = string.Empty;
}

/// <summary>新建装箱单时提交的扩展信息（地址、箱规等）。</summary>
public class PackingCreateExtras
{
    public PackingExtendShipInput? Ship { get; set; }
    public PackingExtendBoxInput? Box { get; set; }
    public string? Comment { get; set; }
    public DateTime? ScheduleShipDate { get; set; }
}

public class PackingExtendShipInput
{
    public string? ShipCompany { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipAttn { get; set; }
    public string? ShipTel { get; set; }
    public string? BillCompany { get; set; }
    public string? BillAddress { get; set; }
    public string? BillAttn { get; set; }
    public string? BillTel { get; set; }
    public string? DeliveryReq { get; set; }
    public short? DeliveryMethod { get; set; }
}

public class PackingExtendBoxInput
{
    public decimal? Nw { get; set; }
    public decimal? Gw { get; set; }
    public string? Dim { get; set; }
    public int? Ctns { get; set; }
}

public class PackingDraftFromStockOutRequestsDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? SalesId { get; set; }
    public string? SalesUserName { get; set; }

    /// <summary>拟生成装箱单的出库类型（与入参顺序第一条出库通知一致）。</summary>
    public short StockOutType { get; set; } = StockOutTypeCode.Sales;

    /// <summary>拟写入装箱单 <c>storage_id</c> 的仓库主键（由送达地域解析）。</summary>
    public string? WarehouseId { get; set; }

    public string? WarehouseName { get; set; }

    public List<PackingDraftLineDto> Lines { get; set; } = new();
}

public class PackingDraftLineDto
{
    public string StockOutRequestId { get; set; } = string.Empty;
    public string? RequestCode { get; set; }
    public string? Pn { get; set; }
    public string? Brand { get; set; }
    public int Qty { get; set; }
    public string? Unit { get; set; }
    public string? SellOrderId { get; set; }
    public string? SellOrderItemId { get; set; }
    public string? SellOrderCode { get; set; }
    public string? SellOrderItemCode { get; set; }
    public string? Remark { get; set; }
}

public class PackingListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public short Status { get; set; }
    public short StockOutType { get; set; }
    public short MaterialType { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesId { get; set; }
    public string? SalesUserName { get; set; }
    public string? StorageId { get; set; }
    public string? WarehouseName { get; set; }
    public int ItemRows { get; set; }
    public string? Comment { get; set; }
    public DateTime? ScheduleShipDate { get; set; }
    public DateTime CreateTime { get; set; }
    public string? CreateByUserId { get; set; }
    public string? CreateUserName { get; set; }
    /// <summary>送货公司名称（<c>packing_extend_ship.ship_company</c>）。</summary>
    public string? ShipCompany { get; set; }
    /// <summary>送货地址（<c>packing_extend_ship.ship_address</c>）。</summary>
    public string? ShipAddress { get; set; }
}

public class PackingItemListRowDto
{
    public string Id { get; set; } = string.Empty;
    public string PackingId { get; set; } = string.Empty;
    public string PackingCode { get; set; } = string.Empty;
    public short PackingStatus { get; set; }
    public string? Pn { get; set; }
    public string? Brand { get; set; }
    public int Qty { get; set; }
    public string? Unit { get; set; }
    public string? SellOrderId { get; set; }
    public string? SellOrderItemId { get; set; }
    public string? SellOrderCode { get; set; }
    public string? SellOrderItemCode { get; set; }
    public string? ItemCode { get; set; }
    public string? CustomerName { get; set; }
    public string? StockOutRequestCode { get; set; }
    public DateTime CreateTime { get; set; }
}

public class PackingCreateResultDto
{
    public string PackingId { get; set; } = string.Empty;
    public string PackingCode { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class PackingDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public short Status { get; set; }
    public short StockOutType { get; set; }
    public short MaterialType { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesId { get; set; }
    public string? SalesUserName { get; set; }
    public int ItemRows { get; set; }
    public DateTime? ScheduleShipDate { get; set; }
    public string? Comment { get; set; }
    public DateTime CreateTime { get; set; }
    public decimal? BoxNw { get; set; }
    public decimal? BoxGw { get; set; }
    public string? BoxDim { get; set; }
    public int? BoxCtns { get; set; }
    public string? ShipCompany { get; set; }
    public string? ShipAddress { get; set; }
    public string? ShipAttn { get; set; }
    public string? ShipTel { get; set; }
    public string? BillCompany { get; set; }
    public string? BillAddress { get; set; }
    public string? BillAttn { get; set; }
    public string? BillTel { get; set; }
    public string? DeliveryReq { get; set; }
    public short? DeliveryMethod { get; set; }
    public List<PackingDetailLineDto> Items { get; set; } = new();
    public List<PackingDetailItemExtendDto> ItemExtends { get; set; } = new();
    public List<PackingStockOutNotifyRowDto> StockOutNotifies { get; set; } = new();
}

/// <summary>装箱单详情内嵌的出库通知列表行。</summary>
public class PackingStockOutNotifyRowDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestCode { get; set; } = string.Empty;
    public short Status { get; set; }
    public string? SalesOrderId { get; set; }
    public string? SalesOrderCode { get; set; }
    public string? SalesOrderItemId { get; set; }
    public string? MaterialModel { get; set; }
    public string? Brand { get; set; }
    public int OutQuantity { get; set; }
    public short RegionType { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesUserName { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime CreateTime { get; set; }
    public string? Remark { get; set; }
}

public class PackingDetailItemExtendDto
{
    public string Id { get; set; } = string.Empty;
    public string PackingItemId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesId { get; set; }
    public string? SalesUserName { get; set; }
    public string? SellOrderId { get; set; }
    public string? SellOrderCode { get; set; }
    public string? SellOrderItemId { get; set; }
    public string? SellOrderItemCode { get; set; }
    public decimal? Price { get; set; }
    public short? PriceCurrency { get; set; }
    public decimal? PriceConvertPrice { get; set; }
    public string? CustomerSo { get; set; }
    public string? CustomerPn { get; set; }
    public string? CustomerBrand { get; set; }
}

public class PackingDetailLineDto
{
    public string Id { get; set; } = string.Empty;
    public string? Pn { get; set; }
    public string? Brand { get; set; }
    public int Qty { get; set; }
    public string? Unit { get; set; }
    public string? SellOrderId { get; set; }
    public string? SellOrderItemId { get; set; }
    public string? SellOrderCode { get; set; }
    public string? SellOrderItemCode { get; set; }
    public string? ItemCode { get; set; }
    public string? StockOutNotifyId { get; set; }
    public string? CustomerSo { get; set; }
    public string? CustomerPn { get; set; }
    public string? CustomerBrand { get; set; }
    public decimal? Price { get; set; }
    public short? PriceCurrency { get; set; }
    public string? Comment { get; set; }
}
