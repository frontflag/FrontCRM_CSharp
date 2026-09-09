using CRM.Core.Constants;

namespace CRM.Core.Interfaces;

public sealed class CommissionCalcRunResult
{
    public DateOnly CalcDate { get; init; }
    public int SalesCount { get; init; }
    public int PurchaseCount { get; init; }
}

public sealed class CommissionLockRunResult
{
    public DateOnly LockDate { get; init; }
    public string Term { get; init; } = string.Empty;
    public DateOnly WindowFrom { get; init; }
    public DateOnly WindowTo { get; init; }
    public int Inserted { get; init; }
    public int SkippedExisting { get; init; }
}

public sealed class CommissionOpenWindowDto
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public string Term { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public sealed class CommissionTermOptionDto
{
    public string Term { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

public sealed class CommissionSummaryDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public short UserLevel { get; init; }
    public int LineCount { get; init; }
    public int MonthCount { get; init; }
    public decimal PeriodGpUsd { get; init; }
    public decimal CommissionUsd { get; init; }
}

public sealed class CommissionMonthDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public short UserLevel { get; init; }
    public string CalcMonth { get; init; } = string.Empty;
    public bool Qualified { get; init; }
    public decimal MonthGpUsd { get; init; }
    public decimal RatePoints { get; init; }
    public decimal CommissionUsd { get; init; }
    public int LineCount { get; init; }
    public decimal? QualifyGpUsd { get; init; }
    public string? Term { get; init; }
}

public sealed class CommissionPersonMonthsDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public short UserLevel { get; init; }
    public IReadOnlyList<CommissionMonthDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalLineCount { get; init; }
    public decimal TotalCommissionUsd { get; init; }
}

public sealed class CommissionLineDto
{
    public string Id { get; init; } = string.Empty;
    public short RoleType { get; init; }
    public string StockOutItemId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public short UserLevel { get; init; }
    public string? VersionId { get; init; }
    public decimal RatePoints { get; init; }
    public decimal GpUsd { get; init; }
    public decimal PeriodGpUsd { get; init; }
    public decimal CommissionUsd { get; init; }
    public string? SellOrderId { get; init; }
    public string? SellOrderCode { get; init; }
    public string? SellOrderItemId { get; init; }
    public string? SellOrderItemCode { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public string StockOutId { get; init; } = string.Empty;
    public string StockOutCode { get; init; } = string.Empty;
    public DateOnly? StockOutDate { get; init; }
    public DateOnly ReceiptDate { get; init; }
    public DateOnly PoolDate { get; init; }
    public string CalcMonth { get; init; } = string.Empty;
    public short EntryKind { get; init; }
    public string? Term { get; init; }
}

public sealed class CommissionPersonLineDto
{
    public string StockOutItemId { get; init; } = string.Empty;
    public string StockOutId { get; init; } = string.Empty;
    public string StockOutCode { get; init; } = string.Empty;
    public DateOnly? StockOutDate { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public short UserLevel { get; init; }
    public bool ReceiptWriteOffDone { get; init; }
    public short ReceiptProgressStatus { get; init; }
    public DateOnly? ReceiptDate { get; init; }
    public int? DaysSinceReceipt { get; init; }
    public bool InCommission { get; init; }
    public decimal GpUsd { get; init; }
    public decimal RatePoints { get; init; }
    public decimal CommissionUsd { get; init; }
    public string CalcMonth { get; init; } = string.Empty;
    public short EntryKind { get; init; }
    public string? SellOrderId { get; init; }
    public string? SellOrderCode { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public string? Term { get; init; }
}

public sealed class CommissionPersonDetailDto
{
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public short UserLevel { get; init; }
    public IReadOnlyList<CommissionPersonLineDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class CommissionPersonQuery
{
    public short RoleType { get; init; }
    public bool Official { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? CalcMonth { get; init; }
    public string? Term { get; init; }
    public string? Keyword { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class CommissionPaged<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class CommissionResultQuery
{
    public short RoleType { get; init; }
    public bool Official { get; init; }
    public string? Keyword { get; init; }
    public string? UserId { get; init; }
    public string? Term { get; init; }
    public DateOnly? PoolDateFrom { get; init; }
    public DateOnly? PoolDateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public interface ICommissionDynamicCalculator
{
    Task<CommissionCalcRunResult> RecalcAsync(DateOnly calcDate, CancellationToken cancellationToken = default);
}

public interface ICommissionLockService
{
    Task<CommissionLockRunResult> LockAsync(DateOnly lockDate, CancellationToken cancellationToken = default);
}

public interface ICommissionResultService
{
    CommissionOpenWindowDto GetOpenWindow(DateOnly? asOf = null);

    Task<CommissionCalcRunResult> RecalcEstimatedAsync(
        DateOnly? calcDate = null,
        CancellationToken cancellationToken = default);

    Task<CommissionLockRunResult> LockOfficialAsync(
        DateOnly? lockDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommissionTermOptionDto>> ListOfficialTermsAsync(
        short roleType,
        CancellationToken cancellationToken = default);

    Task<CommissionPaged<CommissionSummaryDto>> ListSummaryAsync(
        CommissionResultQuery query,
        CancellationToken cancellationToken = default);

    Task<CommissionPersonMonthsDto> ListMonthsAsync(
        CommissionPersonQuery query,
        CancellationToken cancellationToken = default);

    Task<CommissionPaged<CommissionLineDto>> ListLinesAsync(
        CommissionResultQuery query,
        CancellationToken cancellationToken = default);

    Task<CommissionPersonDetailDto> ListPersonLinesAsync(
        CommissionPersonQuery query,
        CancellationToken cancellationToken = default);
}

public static class CommissionPermissionCodes
{
    public const string EstimatedSalesRead = "commission-estimated-sales.read";
    public const string EstimatedPurchaseRead = "commission-estimated-purchase.read";
    public const string OfficialSalesRead = "commission-official-sales.read";
    public const string OfficialPurchaseRead = "commission-official-purchase.read";

    public static string ReadCode(short roleType, bool official) =>
        (roleType, official) switch
        {
            ((short)CommissionRoleType.Sales, false) => EstimatedSalesRead,
            ((short)CommissionRoleType.Purchase, false) => EstimatedPurchaseRead,
            ((short)CommissionRoleType.Sales, true) => OfficialSalesRead,
            ((short)CommissionRoleType.Purchase, true) => OfficialPurchaseRead,
            _ => EstimatedSalesRead
        };
}
