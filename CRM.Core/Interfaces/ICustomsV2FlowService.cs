using CRM.Core.Models.Customs;
using CRM.Core.Models.Inventory;

namespace CRM.Core.Interfaces;

/// <summary>报关 V2 端到端编排（装箱确认预检→拣货保存生成报关单→报关出库→到货→销售 SOR 解锁）。</summary>
public interface ICustomsV2FlowService
{
    Task OnCustomsPackingCreatedAsync(string packingId, string? actingUserId, CancellationToken cancellationToken = default);

    /// <summary>装箱确认前预检：报关公司、境外仓、明细与待报关关联。不生成报关单。</summary>
    Task ValidateCustomsPackingReadyForDeclarationAsync(string packingId, CancellationToken cancellationToken = default);

    /// <summary>已试算或已到货时拒绝改拣货。</summary>
    Task AssertCanReplacePickingForCustomsAsync(string packingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 拣货保存后按拣货行生成或重建报关单（1 拣货行 = 1 报关明细）。
    /// 未试算且未到货时可重建；已试算或已到货则拒绝。
    /// </summary>
    Task<SyncCustomsDeclarationAfterPickingResultDto> SyncCustomsDeclarationAfterPickingAsync(
        string packingId,
        string pickingTaskId,
        string? actingUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 报关装箱单补生成报关单：须已完成拣货；已有有效报关单则跳过。
    /// </summary>
    Task EnsureCustomsDeclarationForPackingAsync(string packingId, string? actingUserId, CancellationToken cancellationToken = default);

    Task EnsureCustomsOutReadyAsync(string customsStockOutRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, CustomsDeclarationItem>> GetDeclarationItemsMapForCustomsStockOutAsync(
        string customsStockOutRequestId,
        CancellationToken cancellationToken = default);

    void ApplyCustomsStockOutExtend(
        StockOutItemExtend ext,
        StockItem layer,
        string? pickingTaskItemId,
        string? packingItemId,
        IReadOnlyDictionary<string, CustomsDeclarationItem> decItemByKey);

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

    Task UpdateDeclarationHeaderAsync(
        string declarationId,
        string? toWarehouseId,
        string? remark,
        string? actingUserId,
        decimal? exchangeRate = null,
        string? customsBrokerId = null,
        bool? costUsdManual = null,
        bool canCorrectLockedCostUsd = false,
        CancellationToken cancellationToken = default);

    Task UpdateDeclarationItemAsync(
        string itemId,
        CustomsDeclarationItemPatch patch,
        string? actingUserId,
        bool canCorrectLockedCostUsd = false,
        CancellationToken cancellationToken = default);

    /// <summary>全单报关费用试算（对齐 EBS §3.3）。结关锁定后仅纠正采购美金价时可放行。</summary>
    Task<RecalculateCustomsDeclarationFeesResultDto> RecalculateDeclarationFeesAsync(
        string declarationId,
        string? actingUserId,
        bool canCorrectLockedCostUsd = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomsDeclarationFieldChangeLogDto>> GetFieldChangeLogsAsync(
        string declarationId,
        CancellationToken cancellationToken = default);

    /// <summary>报关入库过账前校验费用完整性（与到货通知门禁一致）。</summary>
    Task ValidateCustomsStockInFeesAsync(string stockInId, CancellationToken cancellationToken = default);
}

public sealed class SyncCustomsDeclarationAfterPickingResultDto
{
    public const string ActionNone = "None";
    public const string ActionGenerated = "Generated";
    public const string ActionRebuilt = "Rebuilt";
    public const string ActionUnchanged = "Unchanged";

    public string Action { get; set; } = ActionNone;
    public string? DeclarationId { get; set; }
    public string? DeclarationCode { get; set; }
    public int LineCount { get; set; }
}

public sealed class RecalculateCustomsDeclarationFeesResultDto
{
    public string DeclarationId { get; set; } = string.Empty;
    public DateTime FeesCalculatedAtUtc { get; set; }
    public decimal TotalTaxAmount { get; set; }
    public int LineCount { get; set; }
    public int ArrivalNoticesUpdated { get; set; }
    public int StockInItemsUpdated { get; set; }
    public int StockItemLayersUpdated { get; set; }
}

public sealed class CustomsDeclarationFieldChangeLogDto
{
    public string Id { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? ChangedByUserName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ObjectLabel { get; set; } = string.Empty;
}

public sealed class CustomsDeclarationItemPatch
{
    public string? HsCode { get; set; }
    public int? DeclareQty { get; set; }
    public decimal? DeclareUnitPrice { get; set; }
    public decimal? DutyRate { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? OtherFee { get; set; }
    public decimal? InspectionFee { get; set; }
    public decimal? CostUsd { get; set; }
    public bool? CostUsdManual { get; set; }
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
