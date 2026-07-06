using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>报关 V2 端到端编排（装箱确认→报关单→拣货回写→报关出库→到货→销售 SOR 解锁）。</summary>
public interface ICustomsV2FlowService
{
    Task OnCustomsPackingCreatedAsync(string packingId, string? actingUserId, CancellationToken cancellationToken = default);

    Task GenerateDeclarationOnPackingConfirmAsync(string packingId, string? actingUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 报关装箱单补生成报关单：清除已删除报关单的孤儿关联后重新生成；
    /// 若已有完成的拣货任务则回写报关明细源在库行。
    /// </summary>
    Task EnsureCustomsDeclarationForPackingAsync(string packingId, string? actingUserId, CancellationToken cancellationToken = default);

    Task WritebackDeclarationItemsAfterPickingAsync(string packingId, string pickingTaskId, string? actingUserId, CancellationToken cancellationToken = default);

    Task EnsureCustomsOutReadyAsync(string customsStockOutRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, CustomsDeclarationItem>> GetDeclarationItemsMapForCustomsStockOutAsync(
        string customsStockOutRequestId,
        CancellationToken cancellationToken = default);

    void ApplyCustomsStockOutExtend(StockOutItemExtend ext, StockItem layer, string? packingItemId, IReadOnlyDictionary<string, CustomsDeclarationItem> decItemByPackingItemId);

    /// <summary>报关单头人工发起：为本单尚未生成到货通知且已报关出库完成的明细批量创建报关到货通知。</summary>
    Task<CreateCustomsArrivalNotifiesResultDto> CreateCustomsArrivalNotifiesAsync(
        string declarationId,
        string? actingUserId,
        CancellationToken cancellationToken = default);

    Task<CustomsDeclarationArrivalNotifyReadinessDto> GetArrivalNotifyReadinessAsync(
        string declarationId,
        CancellationToken cancellationToken = default);

    Task OnCustomsStockInCompletedAsync(string stockInId, string? actingUserId, CancellationToken cancellationToken = default);

    Task RevertPendlistOnPackingDeleteAsync(IReadOnlyList<string> customsPendlistIds, string? actingUserId, CancellationToken cancellationToken = default);

    Task UpdateDeclarationHeaderAsync(string declarationId, string? toWarehouseId, string? remark, string? actingUserId, CancellationToken cancellationToken = default);

    Task UpdateDeclarationItemAsync(string itemId, CustomsDeclarationItemPatch patch, string? actingUserId, CancellationToken cancellationToken = default);
}

public sealed class CustomsDeclarationItemPatch
{
    public string? HsCode { get; set; }
    public int? DeclareQty { get; set; }
    public decimal? DeclareUnitPrice { get; set; }
    public decimal? DutyAmount { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? CustomsPaymentGoods { get; set; }
    public decimal? CustomsAgencyFee { get; set; }
    public decimal? OtherFee { get; set; }
    public decimal? InspectionFee { get; set; }
    public decimal? TotalValueTax { get; set; }
    public decimal? TaxIncludedUnitPrice { get; set; }
}

public sealed class CreateCustomsArrivalNotifiesResultDto
{
    public string DeclarationId { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public IReadOnlyList<CreatedCustomsArrivalNotifyLineDto> Created { get; set; } = Array.Empty<CreatedCustomsArrivalNotifyLineDto>();
}

public sealed class CreatedCustomsArrivalNotifyLineDto
{
    public string NoticeId { get; set; } = string.Empty;
    public string NoticeCode { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public string CustomsDeclarationItemId { get; set; } = string.Empty;
}

public sealed class CustomsDeclarationArrivalNotifyReadinessDto
{
    public bool CanCreate { get; set; }
    public int PendingCount { get; set; }
    public int ExistingCount { get; set; }
    public IReadOnlyList<string> ExistingNoticeCodes { get; set; } = Array.Empty<string>();
    public string? BlockReason { get; set; }
}
